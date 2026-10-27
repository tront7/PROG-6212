using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Services;
using System.Net.Mail;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<UsersController> _logger;

    public UsersController(RaceDayDbContext context, IBlobStorageService blobStorage, ILogger<UsersController> logger)
    {
        _context = context;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Returns the profile of the currently logged-in user.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var user = await _context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);

        if (user is null) return NotFound();

        return Ok(ToProfile(user));
    }

    /// <summary>Returns a specific user's profile.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user is null) return NotFound();

        return Ok(ToProfile(user));
    }

    /// <summary>Updates the logged-in user's own profile. Cannot be used to edit another user.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserProfileRequest request)
    {
        if (id != CurrentUserId) return Forbid();

        if (string.IsNullOrWhiteSpace(request.FullName))
            return BadRequest("Full name is required.");

        if (string.IsNullOrWhiteSpace(request.Email) || !MailAddress.TryCreate(request.Email.Trim(), out _))
            return BadRequest("A valid email address is required.");

        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound();

        var email = request.Email.Trim();
        var emailTaken = await _context.Users.AnyAsync(u => u.Email == email && u.UserId != id);
        if (emailTaken) return Conflict("That email address is already used by another account.");

        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.Email = email;

        await _context.SaveChangesAsync();

        return Ok(new UpdateUserProfileRequest(user.FullName, user.PhoneNumber, user.Email));
    }

    /// <summary>Uploads the logged-in user's profile picture to Azure Blob Storage.</summary>
    [HttpPost("me/profile-picture")]
    public async Task<IActionResult> UploadProfilePicture(IFormFile file)
    {
        var validationError = ImageValidator.Validate(file);
        if (validationError is not null) return BadRequest(validationError);

        if (!_blobStorage.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Image storage is not configured on the server.");

        var user = await _context.Users.FindAsync(CurrentUserId);
        if (user is null) return NotFound();

        try
        {
            var url = await _blobStorage.UploadImageAsync(file, "profile-pictures");
            user.ProfilePictureUrl = url;
            await _context.SaveChangesAsync();
            return Ok(new ImageUploadResponse(url));
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Blob Storage upload failed for user {UserId}", user.UserId);
            return StatusCode(StatusCodes.Status502BadGateway,
                "The image could not be saved to Azure Blob Storage. Please try again later.");
        }
    }

    private static UserProfileResponse ToProfile(Models.User user) =>
        new(user.UserId, user.FullName, user.Email, user.PhoneNumber, user.Role.RoleName, user.CreatedAt, user.ProfilePictureUrl);
}
