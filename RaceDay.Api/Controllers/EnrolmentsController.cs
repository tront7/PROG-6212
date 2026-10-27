using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.Linq.Expressions;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController]
public class EnrolmentsController : ControllerBase
{
    private static readonly string[] Statuses = { "Confirmed", "Pending", "Cancelled" };

    private readonly RaceDayDbContext _context;

    public EnrolmentsController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Shared projection: enrolment + event + category + participant + any captured result.
    private static readonly Expression<Func<Enrolment, EnrolmentResponse>> ToResponse = e => new EnrolmentResponse(
        e.EnrolmentId, e.UserId, e.EventId, e.CategoryId, e.EnrolmentDate, e.Status,
        e.Event.EventName, e.Event.EventDate, e.Category.CategoryName,
        e.User.FullName, e.User.Email,
        e.Result == null ? (int?)null : e.Result.ResultId,
        e.Result == null ? (TimeSpan?)null : e.Result.FinishTime,
        e.Result == null ? (int?)null : e.Result.Position,
        e.Result == null ? null : e.Result.Status);

    /// <summary>Enrols the logged-in participant into an event category.</summary>
    [HttpPost("api/enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enrol(CreateEnrolmentRequest request)
    {
        var category = await _context.Categories
            .Include(c => c.Event)
            .Include(c => c.Enrolments)
            .FirstOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == request.EventId);

        if (category is null) return NotFound("Event or category does not exist.");

        if (category.Event.EventDate < DateTime.UtcNow.Date)
            return BadRequest("This event has already taken place.");

        var userId = CurrentUserId;

        var alreadyEnrolled = await _context.Enrolments
            .AnyAsync(e => e.UserId == userId && e.CategoryId == request.CategoryId);
        if (alreadyEnrolled) return Conflict("You are already enrolled in this category.");

        if (category.Enrolments.Count(e => e.Status != "Cancelled") >= category.MaxParticipants)
            return Conflict("This category is full.");

        var enrolment = new Enrolment
        {
            UserId = userId,
            EventId = request.EventId,
            CategoryId = request.CategoryId
        };

        _context.Enrolments.Add(enrolment);
        await _context.SaveChangesAsync();

        var response = await _context.Enrolments
            .Where(e => e.EnrolmentId == enrolment.EnrolmentId)
            .Select(ToResponse)
            .FirstAsync();

        return StatusCode(StatusCodes.Status201Created, response);
    }

    /// <summary>Lists the logged-in participant's own enrolments.</summary>
    [HttpGet("api/enrolments/me")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var userId = CurrentUserId;

        var enrolments = await _context.Enrolments
            .Where(e => e.UserId == userId)
            .OrderBy(e => e.Event.EventDate)
            .Select(ToResponse)
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
            .OrderBy(e => e.Category.CategoryName)
            .ThenBy(e => e.User.FullName)
            .Select(ToResponse)
            .ToListAsync();

        return Ok(enrolments);
    }

    /// <summary>Sets an enrolment's status (Confirmed, Pending or Cancelled). Only the owning organiser may do this.</summary>
    [HttpPut("api/enrolments/{id:int}/status")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateStatus(int id, UpdateEnrolmentStatusRequest request)
    {
        var status = Statuses.FirstOrDefault(s => s.Equals(request.Status?.Trim(), StringComparison.OrdinalIgnoreCase));
        if (status is null) return BadRequest("Status must be Confirmed, Pending or Cancelled.");

        var enrolment = await _context.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return NotFound();
        if (enrolment.Event.OrganiserId != CurrentUserId) return Forbid();

        enrolment.Status = status;
        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>Cancels (removes) the logged-in participant's own enrolment.</summary>
    [HttpDelete("api/enrolments/{id:int}")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> CancelEnrolment(int id)
    {
        var enrolment = await _context.Enrolments.Include(e => e.Result).FirstOrDefaultAsync(e => e.EnrolmentId == id);
        if (enrolment is null) return NotFound();
        if (enrolment.UserId != CurrentUserId) return Forbid();
        if (enrolment.Result is not null)
            return Conflict("A result has already been recorded for this enrolment, so it cannot be cancelled.");

        _context.Enrolments.Remove(enrolment);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
