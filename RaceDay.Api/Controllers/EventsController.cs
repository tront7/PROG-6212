using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private readonly RaceDayDbContext _context;

    public EventsController(RaceDayDbContext context)
    {
        _context = context;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Lists all events. Public — no login required.</summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetEvents()
    {
        var events = await _context.Events.Include(e => e.Organiser)
            .Select(e => new EventResponse(e.EventId, e.EventName, e.EventDate, e.Location, e.Description, e.OrganiserId, e.Organiser.FullName))
            .ToListAsync();

        return Ok(events);
    }

    /// <summary>Returns a single event's details. Public — no login required.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _context.Events.Include(e => e.Organiser)
            .FirstOrDefaultAsync(e => e.EventId == id);

        if (ev is null) return NotFound();

        return Ok(new EventResponse(ev.EventId, ev.EventName, ev.EventDate, ev.Location, ev.Description, ev.OrganiserId, ev.Organiser.FullName));
    }

    /// <summary>Creates a new event owned by the logged-in organiser.</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateEvent(CreateEventRequest request)
    {
        var ev = new Event
        {
            EventName = request.EventName,
            EventDate = request.EventDate,
            Location = request.Location,
            Description = request.Description,
            OrganiserId = CurrentUserId
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        var organiserName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId },
            new EventResponse(ev.EventId, ev.EventName, ev.EventDate, ev.Location, ev.Description, ev.OrganiserId, organiserName));
    }

    /// <summary>Updates an event. Only the organiser who created it may update it.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateEvent(int id, UpdateEventRequest request)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        ev.EventName = request.EventName;
        ev.EventDate = request.EventDate;
        ev.Location = request.Location;
        ev.Description = request.Description;

        await _context.SaveChangesAsync();
        return Ok();
    }

    /// <summary>Deletes an event. Only the organiser who created it may delete it.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> DeleteEvent(int id)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();
        return NoContent();
    }
}
