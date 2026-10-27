using Microsoft.AspNetCore.Mvc;
using RaceDay.Web.Filters;
using RaceDay.Web.Models;
using RaceDay.Web.Services;

namespace RaceDay.Web.Controllers;

/// <summary>Everything an Organiser can do. The whole controller is restricted to the Organiser role.</summary>
[SessionAuthorize("Organiser")]
public class OrganiserController : AppController
{
    private static readonly string[] AllowedImageTypes = { "image/jpeg", "image/png", "image/webp", "image/gif" };
    private const long MaxImageBytes = 5 * 1024 * 1024;

    public OrganiserController(IApiClient api) : base(api) { }

    // =====================================================================
    // Dashboard
    // =====================================================================

    [HttpGet]
    public async Task<IActionResult> Dashboard()
    {
        var result = await Api.GetAsync<List<EventSummaryDto>>("api/events/mine");
        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (!result.Success) ViewData["Error"] = result.Error;

        return View(new OrganiserDashboardViewModel { Events = result.Data ?? new() });
    }

    // =====================================================================
    // Events
    // =====================================================================

    [HttpGet]
    public async Task<IActionResult> Events()
    {
        var result = await Api.GetAsync<List<EventSummaryDto>>("api/events/mine");
        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (!result.Success) ViewData["Error"] = result.Error;

        return View(result.Data ?? new List<EventSummaryDto>());
    }

    [HttpGet]
    public IActionResult CreateEvent() => View("EventForm", new EventFormViewModel());

    [HttpPost]
    public async Task<IActionResult> CreateEvent(EventFormViewModel model)
    {
        ValidateBanner(model);
        if (!ModelState.IsValid) return View("EventForm", model);

        var create = await Api.PostAsync<EventDto>("api/events", ToRequestBody(model));
        var redirect = RedirectOnAuthFailure(create);
        if (redirect is not null) return redirect;

        if (!create.Success || create.Data is null)
        {
            ModelState.AddModelError(string.Empty, create.Error ?? "We couldn't create the event.");
            return View("EventForm", model);
        }

        var bannerError = model.BannerFile is null ? null : await UploadBannerAsync(create.Data.EventId, model.BannerFile);
        if (bannerError is null) FlashSuccess($"\"{model.EventName}\" has been created.");
        else FlashError($"\"{model.EventName}\" was created, but the banner couldn't be uploaded: {bannerError}");

        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id)
    {
        var result = await Api.GetAsync<EventDto>($"api/events/{id}");

        if (result.IsNotFound)
        {
            FlashError("That event no longer exists.");
            return RedirectToAction(nameof(Events));
        }

        if (!result.Success || result.Data is null)
        {
            FlashError(result.Error ?? "We couldn't load that event.");
            return RedirectToAction(nameof(Events));
        }

        if (result.Data.OrganiserId != CurrentUserId) return RedirectToAction("AccessDenied", "Account");

        return View("EventForm", new EventFormViewModel
        {
            EventId = result.Data.EventId,
            EventName = result.Data.EventName,
            Description = result.Data.Description,
            EventDate = result.Data.EventDate,
            Location = result.Data.Location,
            DistanceKm = result.Data.DistanceKm,
            EventType = result.Data.EventType,
            ExistingBannerUrl = result.Data.BannerImageUrl
        });
    }

    [HttpPost]
    public async Task<IActionResult> EditEvent(int id, EventFormViewModel model)
    {
        model.EventId = id;

        ValidateBanner(model);
        if (!ModelState.IsValid) return View("EventForm", model);

        var update = await Api.PutAsync<Empty>($"api/events/{id}", ToRequestBody(model));
        var redirect = RedirectOnAuthFailure(update);
        if (redirect is not null) return redirect;

        if (update.IsNotFound)
        {
            FlashError("That event no longer exists.");
            return RedirectToAction(nameof(Events));
        }

        if (!update.Success)
        {
            ModelState.AddModelError(string.Empty, update.Error ?? "We couldn't save your changes.");
            return View("EventForm", model);
        }

        var bannerError = model.BannerFile is null ? null : await UploadBannerAsync(id, model.BannerFile);
        if (bannerError is null) FlashSuccess($"\"{model.EventName}\" has been updated.");
        else FlashError($"Your changes were saved, but the banner couldn't be uploaded: {bannerError}");

        return RedirectToAction(nameof(Events));
    }

    [HttpPost]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var result = await Api.DeleteAsync($"api/events/{id}");
        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (result.Success) FlashSuccess("The event has been deleted.");
        else FlashError(result.Error ?? "We couldn't delete that event.");

        return RedirectToAction(nameof(Events));
    }

    // =====================================================================
    // Categories
    // =====================================================================

    [HttpGet]
    public async Task<IActionResult> Categories(int? eventId, int? editId)
    {
        if (eventId is null)
        {
            return await PickEventAsync("Categories", "Categories",
                "Choose an event to add or edit its age and distance categories.", "Manage categories");
        }

        return await RenderCategoriesAsync(eventId.Value, editId, null);
    }

    [HttpPost]
    public async Task<IActionResult> SaveCategory([Bind(Prefix = nameof(CategoriesPageViewModel.Form))] CategoryFormViewModel model)
    {
        if (!ModelState.IsValid) return await RenderCategoriesAsync(model.EventId, null, model);

        var body = new
        {
            categoryName = model.CategoryName,
            distanceKm = model.DistanceKm,
            maxParticipants = model.MaxParticipants,
            price = model.Price
        };

        var result = model.IsEdit
            ? await Api.PutAsync<Empty>($"api/categories/{model.CategoryId}", body)
            : await Api.PostAsync<Empty>($"api/events/{model.EventId}/categories", body);

        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error ?? "We couldn't save that category.");
            return await RenderCategoriesAsync(model.EventId, null, model);
        }

        FlashSuccess(model.IsEdit ? "Category updated." : "Category added.");
        return RedirectToAction(nameof(Categories), new { eventId = model.EventId });
    }

    [HttpPost]
    public async Task<IActionResult> DeleteCategory(int id, int eventId)
    {
        var result = await Api.DeleteAsync($"api/categories/{id}");
        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (result.Success) FlashSuccess("Category deleted.");
        else FlashError(result.Error ?? "We couldn't delete that category.");

        return RedirectToAction(nameof(Categories), new { eventId });
    }

    private async Task<IActionResult> RenderCategoriesAsync(int eventId, int? editId, CategoryFormViewModel? form)
    {
        var ev = await Api.GetAsync<EventDto>($"api/events/{eventId}");
        if (ev.IsNotFound)
        {
            FlashError("That event no longer exists.");
            return RedirectToAction(nameof(Events));
        }

        if (!ev.Success || ev.Data is null)
        {
            FlashError(ev.Error ?? "We couldn't load that event.");
            return RedirectToAction(nameof(Events));
        }

        if (ev.Data.OrganiserId != CurrentUserId) return RedirectToAction("AccessDenied", "Account");

        var categories = await Api.GetAsync<List<CategoryDto>>($"api/events/{eventId}/categories");
        if (!categories.Success) ViewData["Error"] = categories.Error;

        var page = new CategoriesPageViewModel
        {
            Event = ev.Data,
            Categories = categories.Data ?? new List<CategoryDto>()
        };

        if (form is not null)
        {
            page.Form = form;
        }
        else
        {
            var editing = editId is null ? null : page.Categories.FirstOrDefault(c => c.CategoryId == editId);
            page.Form = editing is null
                ? new CategoryFormViewModel { EventId = eventId }
                : new CategoryFormViewModel
                {
                    CategoryId = editing.CategoryId,
                    EventId = eventId,
                    CategoryName = editing.CategoryName,
                    DistanceKm = editing.DistanceKm,
                    MaxParticipants = editing.MaxParticipants,
                    Price = editing.Price
                };
        }

        return View("Categories", page);
    }

    // =====================================================================
    // Enrolments
    // =====================================================================

    [HttpGet]
    public async Task<IActionResult> Enrolments(int? eventId)
    {
        if (eventId is null)
        {
            return await PickEventAsync("Enrolments", "Enrolments",
                "Choose an event to see who has enrolled, in which category, and their enrolment status.", "View enrolments");
        }

        return await RenderEnrolmentsPageAsync(eventId.Value, "Enrolments");
    }

    [HttpPost]
    public async Task<IActionResult> UpdateEnrolmentStatus(int enrolmentId, int eventId, string status)
    {
        var result = await Api.PutAsync<Empty>($"api/enrolments/{enrolmentId}/status", new { status });
        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (result.Success) FlashSuccess($"Enrolment marked as {status}.");
        else FlashError(result.Error ?? "We couldn't update that enrolment.");

        return RedirectToAction(nameof(Enrolments), new { eventId });
    }

    // =====================================================================
    // Results
    // =====================================================================

    [HttpGet]
    public async Task<IActionResult> Results(int? eventId)
    {
        if (eventId is null)
        {
            return await PickEventAsync("Results", "Results",
                "Choose an event to capture and publish finish times and finishing positions.", "Enter results");
        }

        return await RenderEnrolmentsPageAsync(eventId.Value, "Results");
    }

    [HttpPost]
    public async Task<IActionResult> SaveResult(ResultFormViewModel model)
    {
        if (model.Status == "Finished")
        {
            if (model.FinishTime is null && !ModelState.ContainsKey(nameof(model.FinishTime)))
            {
                ModelState.AddModelError(nameof(model.FinishTime), "A finish time is required for finished participants.");
            }

            if ((model.Position is null || model.Position < 1) && !ModelState.ContainsKey(nameof(model.Position)))
            {
                ModelState.AddModelError(nameof(model.Position), "A finishing position is required for finished participants.");
            }
        }

        if (!ModelState.IsValid)
        {
            FlashError("Please check the result: the finish time must look like 00:52:30 (hours:minutes:seconds) " +
                       "and the position must be a whole number, 1 or higher.");
            return RedirectToAction(nameof(Results), new { eventId = model.EventId });
        }

        ApiResult<Empty> result;
        if (model.ResultId is null)
        {
            result = await Api.PostAsync<Empty>("api/results", new
            {
                enrolmentId = model.EnrolmentId,
                finishTime = model.FinishTime,
                position = model.Position,
                status = model.Status
            });
        }
        else
        {
            result = await Api.PutAsync<Empty>($"api/results/{model.ResultId}", new
            {
                finishTime = model.FinishTime,
                position = model.Position,
                status = model.Status
            });
        }

        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (result.Success) FlashSuccess("Result saved and published.");
        else FlashError(result.Error ?? "We couldn't save that result.");

        return RedirectToAction(nameof(Results), new { eventId = model.EventId });
    }

    // =====================================================================
    // Helpers
    // =====================================================================

    private async Task<IActionResult> RenderEnrolmentsPageAsync(int eventId, string viewName)
    {
        var ev = await Api.GetAsync<EventDto>($"api/events/{eventId}");
        if (ev.IsNotFound)
        {
            FlashError("That event no longer exists.");
            return RedirectToAction(nameof(Events));
        }

        if (!ev.Success || ev.Data is null)
        {
            FlashError(ev.Error ?? "We couldn't load that event.");
            return RedirectToAction(nameof(Events));
        }

        var enrolments = await Api.GetAsync<List<EnrolmentDto>>($"api/events/{eventId}/enrolments");
        var redirect = RedirectOnAuthFailure(enrolments);
        if (redirect is not null) return redirect;

        if (!enrolments.Success) ViewData["Error"] = enrolments.Error;

        return View(viewName, new EnrolmentsPageViewModel
        {
            Event = ev.Data,
            Enrolments = enrolments.Data ?? new List<EnrolmentDto>()
        });
    }

    private async Task<IActionResult> PickEventAsync(string targetAction, string title, string description, string buttonText)
    {
        var result = await Api.GetAsync<List<EventSummaryDto>>("api/events/mine");
        var redirect = RedirectOnAuthFailure(result);
        if (redirect is not null) return redirect;

        if (!result.Success) ViewData["Error"] = result.Error;

        return View("SelectEvent", new EventPickerViewModel
        {
            Title = title,
            Description = description,
            TargetAction = targetAction,
            ButtonText = buttonText,
            Events = result.Data ?? new List<EventSummaryDto>()
        });
    }

    private async Task<string?> UploadBannerAsync(int eventId, IFormFile file)
    {
        var upload = await Api.UploadAsync<ImageUploadDto>($"api/events/{eventId}/banner", file);
        return upload.Success ? null : upload.Error ?? "please try again.";
    }

    private void ValidateBanner(EventFormViewModel model)
    {
        var file = model.BannerFile;
        if (file is null) return;

        if (file.Length > MaxImageBytes)
            ModelState.AddModelError(nameof(model.BannerFile), "The banner must be 5 MB or smaller.");
        else if (!AllowedImageTypes.Contains(file.ContentType.ToLowerInvariant()))
            ModelState.AddModelError(nameof(model.BannerFile), "Please choose a JPG, PNG, WEBP or GIF image.");
    }

    private static object ToRequestBody(EventFormViewModel model) => new
    {
        eventName = model.EventName,
        eventDate = model.EventDate,
        location = model.Location,
        description = model.Description,
        distanceKm = model.DistanceKm,
        eventType = model.EventType
    };
}
