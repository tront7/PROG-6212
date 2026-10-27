using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Helpers;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

public class AccountController : AppController
{
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public AccountController(IApiClient api) : base(api) { }

    // ---------- Register ----------

    [HttpGet]
    public IActionResult Register()
    {
        if (HttpContext.Session.IsAuthenticated()) return RedirectToRoleHome();
        return View(new RegisterViewModel());
    }

    [HttpPost]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.PostAsync<AuthDto>("api/auth/register", new
        {
            fullName = model.FullName,
            email = model.Email,
            password = model.Password,
            role = model.Role,
            phoneNumber = model.PhoneNumber
        });

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't create your account.");
            return View(model);
        }

        FlashSuccess("Your account has been created. Please log in.");
        return RedirectToAction(nameof(Login));
    }

    // ---------- Login / Logout ----------

    [HttpGet]
    public IActionResult Login(string? returnUrl = null)
    {
        if (HttpContext.Session.IsAuthenticated()) return RedirectToRoleHome();
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var result = await Api.PostAsync<AuthDto>("api/auth/login", new { email = model.Email, password = model.Password });

        if (!result.Success || result.Data is null)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't log you in.");
            return View(model);
        }

        // The ApiClient has already stored the API's session cookie; now store who this user is.
        HttpContext.Session.StoreUser(result.Data);

        if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
        {
            var wantsOrganiserArea = model.ReturnUrl.StartsWith("/Organiser", StringComparison.OrdinalIgnoreCase);
            var isOrganiser = result.Data.Role == "Organiser";
            if (wantsOrganiserArea == isOrganiser) return LocalRedirect(model.ReturnUrl);
        }

        return RedirectToRoleHome();
    }

    [HttpPost]
    [SessionAuthorize]
    public async Task<IActionResult> Logout()
    {
        await Api.PostAsync<Empty>("api/auth/logout"); // best effort; the local session is cleared regardless
        HttpContext.Session.Clear();
        FlashSuccess("You have been logged out.");
        return RedirectToAction(nameof(Login));
    }

    [HttpGet]
    public IActionResult AccessDenied() => View();

    // ---------- Profile (both roles) ----------

    [HttpGet]
    [SessionAuthorize]
    public async Task<IActionResult> Profile()
    {
        var result = await Api.GetAsync<UserProfileDto>("api/users/me");
        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (!result.Success || result.Data is null)
        {
            FlashError(result.Error ?? "We couldn't load your profile.");
            return RedirectToRoleHome();
        }

        return View(ToViewModel(result.Data));
    }

    [HttpPost]
    [SessionAuthorize]
    public async Task<IActionResult> Profile(ProfileViewModel model)
    {
        if (model.PictureFile is not null)
        {
            var pictureError = ValidatePicture(model.PictureFile);
            if (pictureError is not null) ModelState.AddModelError(nameof(model.PictureFile), pictureError);
        }

        if (!ModelState.IsValid)
        {
            await FillDisplayFieldsAsync(model);
            return View(model);
        }

        var update = await Api.PutAsync<Empty>($"api/users/{CurrentUserId}", new
        {
            fullName = model.FullName,
            phoneNumber = model.PhoneNumber,
            email = model.Email
        });

        var redirect = RedirectOnAuthFailure(update);
        if (redirect is not null) return redirect;

        if (!update.Success)
        {
            ModelState.AddModelError(string.Empty, update.Error ?? "We couldn't update your profile.");
            await FillDisplayFieldsAsync(model);
            return View(model);
        }

        HttpContext.Session.SetString(SessionKeys.FullName, model.FullName.Trim());

        if (model.PictureFile is not null)
        {
            var upload = await Api.UploadAsync<ImageUploadDto>("api/users/me/profile-picture", model.PictureFile);
            if (upload.Success && upload.Data is not null)
            {
                HttpContext.Session.SetPicture(upload.Data.Url);
            }
            else
            {
                FlashError("Your details were saved, but the picture couldn't be uploaded: " + (upload.Error ?? "please try again."));
                return RedirectToAction(nameof(Profile));
            }
        }

        FlashSuccess("Your profile has been updated.");
        return RedirectToAction(nameof(Profile));
    }

    private async Task FillDisplayFieldsAsync(ProfileViewModel model)
    {
        var current = await Api.GetAsync<UserProfileDto>("api/users/me");
        if (current.Success && current.Data is not null)
        {
            model.ProfilePictureUrl = current.Data.ProfilePictureUrl;
            model.Role = current.Data.Role;
            model.MemberSince = current.Data.CreatedAt;
        }
    }

    private static ProfileViewModel ToViewModel(UserProfileDto profile) => new()
    {
        FullName = profile.FullName,
        Email = profile.Email,
        PhoneNumber = profile.PhoneNumber,
        ProfilePictureUrl = profile.ProfilePictureUrl,
        Role = profile.Role,
        MemberSince = profile.CreatedAt
    };

    private static string? ValidatePicture(IFormFile file)
    {
        if (file.Length == 0) return "That file is empty.";
        if (file.Length > MaxImageBytes) return "The picture must be 5 MB or smaller.";
        if (!AllowedImageTypes.Contains(file.ContentType.ToLowerInvariant())) return "Please choose a JPG, PNG, WEBP or GIF image.";
        return null;
    }
}
