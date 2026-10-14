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

        var result = new Result
        {
            EnrolmentId = request.EnrolmentId,
            FinishTime = request.FinishTime,
            Position = request.Position,
            Status = request.Status
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

        result.FinishTime = request.FinishTime;
        result.Position = request.Position;
        result.Status = request.Status;

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>Lists the logged-in participant's own results history.</summary>
    [HttpGet("api/results/me")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> GetMyResults()
    {
        var results = await _context.Results
            .Include(r => r.Enrolment)
            .Where(r => r.Enrolment.UserId == CurrentUserId)
            .Select(r => new ResultResponse(r.ResultId, r.EnrolmentId, r.FinishTime, r.Position, r.Status, r.RecordedAt))
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
            .Include(r => r.Enrolment)
            .Where(r => r.Enrolment.EventId == eventId)
            .Select(r => new ResultResponse(r.ResultId, r.EnrolmentId, r.FinishTime, r.Position, r.Status, r.RecordedAt))
            .ToListAsync();

        return Ok(results);
    }
}
