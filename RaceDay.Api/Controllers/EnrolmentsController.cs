using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController]
public class EnrolmentsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Enrols the logged-in participant into an event category.</summary>
    [HttpPost("api/enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enrol(CreateEnrolmentRequest request)
    {
        var category = await _context.Categories
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == request.EventId);

        if (category is null) return NotFound("Event or category does not exist.");

        var alreadyEnrolled = await _context.Enrolments
            .AnyAsync(e => e.UserId == CurrentUserId && e.CategoryId == request.CategoryId);
        if (alreadyEnrolled) return Conflict("You are already enrolled in this category.");

        if (category.Enrolments.Count >= category.MaxParticipants)
            return Conflict("This category is full.");

        var enrolment = new Enrolment
        {
            UserId = CurrentUserId,
            EventId = request.EventId,
            CategoryId = request.CategoryId
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created,
            new EnrolmentResponse(enrolment.EnrolmentId, enrolment.UserId, enrolment.EventId, enrolment.CategoryId, enrolment.EnrolmentDate, enrolment.Status));
    }

    /// <summary>Lists the logged-in participant's own enrolments.</summary>
    [HttpGet("api/enrolments/me")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var enrolments = await _context.Enrolments
            .Where(e => e.UserId == CurrentUserId)
            .Select(e => new EnrolmentResponse(e.EnrolmentId, e.UserId, e.EventId, e.CategoryId, e.EnrolmentDate, e.Status))
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Lists all enrolments for an event. Only the owning organiser may view these.</summary>
    [HttpGet("api/events/{eventId:int}/enrolments")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var ev = await _context.Events.FindAsync(eventId);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var enrolments = await _context.Enrolments
            .Where(e => e.EventId == eventId)
            .Select(e => new EnrolmentResponse(e.EnrolmentId, e.UserId, e.EventId, e.CategoryId, e.EnrolmentDate, e.Status))
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Cancels the logged-in participant's own enrolment.</summary>
    [HttpDelete("api/enrolments/{id:int}")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _context.Enrolments.FindAsync(id);
        if (enrolment is null) return NotFound();
        if (enrolment.UserId != CurrentUserId) return Forbid();

        _context.Enrolments.Remove(enrolment);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
