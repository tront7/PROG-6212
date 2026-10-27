using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController]
public class ResultsController : ControllerBase
{
    private static readonly string[] ResultStatuses = { "Finished", "DNF", "DQ" };

    private readonly RaceDayDbContext _context;

    public ResultsController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Captures a result for a participant's enrolment. Only the owning organiser may do this.</summary>
    [HttpPost("api/results")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateResult(CreateResultRequest request)
    {
        var enrolment = await _context.Enrolments
            .Include(e => e.Event)
            .Include(e => e.Result)
            .FirstOrDefaultAsync(e => e.EnrolmentId == request.EnrolmentId);

        if (enrolment is null) return NotFound("Enrolment does not exist.");
        if (enrolment.Event.OrganiserId != CurrentUserId) return Forbid();
        if (enrolment.Result is not null) return Conflict("A result has already been captured for this enrolment.");

        var error = Validate(request.FinishTime, request.Position, request.Status, out var status);
        if (error is not null) return BadRequest(error);

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = status
        };

        _context.Results.Add(result);
        await _context.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created,
            new ResultResponse(result.ResultId, result.EnrolmentId, result.FinishTime, result.Position, result.Status, result.RecordedAt));
    }

    /// <summary>Updates a previously captured result. Only the owning organiser may do this.</summary>
    [HttpPut("api/results/{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateResult(int id, UpdateResultRequest request)
    {
        var result = await _context.Results
            .Include(r => r.Enrolment).ThenInclude(e => e.Event)
            .FirstOrDefaultAsync(r => r.ResultId == id);

        if (result is null) return NotFound();
        if (result.Enrolment.Event.OrganiserId != CurrentUserId) return Forbid();

        var error = Validate(request.FinishTime, request.Position, request.Status, out var status);
        if (error is not null) return BadRequest(error);

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = status;

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>Lists the logged-in participant's own race history, including their position out of all finishers.</summary>
    [HttpGet("api/results/me")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> GetMyResults()
    {
        var userId = CurrentUserId;

        var results = await _context.Results
            .Where(r => r.Enrolment.UserId == userId)
            .OrderByDescending(r => r.Enrolment.Event.EventDate)
            .Select(r => new MyResultResponse(
                r.ResultId,
                r.EnrolmentId,
                r.Enrolment.EventId,
                r.Enrolment.Event.EventName,
                r.Enrolment.Event.EventDate,
                r.Enrolment.Category.CategoryName,
                r.FinishTime,
                r.Position,
                r.Enrolment.Category.Enrolments.Count(en => en.Result != null && en.Result.Status == "Finished"),
                r.Status,
                r.RecordedAt))
            .ToListAsync();

        return Ok(results);
    }

    /// <summary>Lists all results for an event (public leaderboard).</summary>
    [HttpGet("api/events/{eventId:int}/results")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        var eventExists = await _context.Events.AnyAsync(e => e.EventId == eventId);
        if (!eventExists) return NotFound();

        var results = await _context.Results
            .Where(r => r.Enrolment.EventId == eventId)
            .OrderBy(r => r.Enrolment.Category.CategoryName)
            .ThenBy(r => r.Position ?? int.MaxValue)
            .Select(r => new EventResultResponse(
                r.ResultId, r.EnrolmentId, r.Enrolment.User.FullName, r.Enrolment.Category.CategoryName,
                r.FinishTime, r.Position, r.Status))
            .ToListAsync();

        return Ok(results);
    }

    private static string? Validate(TimeSpan? finishTime, int? position, string? statusInput, out string status)
    {
        status = ResultStatuses.FirstOrDefault(s => s.Equals(statusInput?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

        if (status.Length == 0) return "Result status must be Finished, DNF or DQ.";
        if (finishTime is not null && finishTime < TimeSpan.Zero) return "Finish time cannot be negative.";
        if (position is not null && position < 1) return "Finishing position must be 1 or higher.";
        if (status == "Finished")
        {
            if (finishTime is null) return "A finish time is required for finished participants.";
            if (position is null || position < 1) return "A finishing position is required for finished participants.";
        }

        return null;
    }
}
