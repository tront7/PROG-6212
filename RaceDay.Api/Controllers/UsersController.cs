using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public UsersController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Returns the profile of the currently logged-in user.</summary>
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var user = await _context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == CurrentUserId);

        if (user is null) return NotFound();

        return Ok(new UserProfileResponse(user.UserId, user.FullName, user.Email, user.PhoneNumber, user.Role.RoleName, user.CreatedAt));
    }

    /// <summary>Returns a specific user's profile.</summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetUser(int id)
    {
        var user = await _context.Users.Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == id);

        if (user is null) return NotFound();

        return Ok(new UserProfileResponse(user.UserId, user.FullName, user.Email, user.PhoneNumber, user.Role.RoleName, user.CreatedAt));
    }

    /// <summary>Updates the logged-in user's own profile. Cannot be used to edit another user.</summary>
    [HttpPut("{id:int}")]
    public async Task<IActionResult> UpdateUser(int id, UpdateUserProfileRequest request)
    {
        if (id != CurrentUserId) return Forbid();

        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound();

        user.FullName = request.FullName;
        user.PhoneNumber = request.PhoneNumber;
        user.Email = request.Email;

        await _context.SaveChangesAsync();

        return Ok(new UpdateUserProfileRequest(user.FullName, user.PhoneNumber, user.Email));
    }
}
