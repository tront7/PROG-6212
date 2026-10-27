using Azure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.DTOs;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using System.Linq.Expressions;
using System.Security.Claims;

namespace RaceDay.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController : ControllerBase
{
    private static readonly string[] EventTypes = { "Run", "Walk", "Cycle" };

    private readonly RaceDayDbContext _context;
    private readonly IBlobStorageService _blobStorage;
    private readonly ILogger<EventsController> _logger;

    public EventsController(RaceDayDbContext context, IBlobStorageService blobStorage, ILogger<EventsController> logger)
    {
        _context = context;
        _blobStorage = blobStorage;
        _logger = logger;
    }

    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Shared projection so every endpoint returns events in exactly the same shape.
    private static readonly Expression<Func<Event, EventResponse>> ToResponse = e => new EventResponse(
        e.EventId, e.EventName, e.EventDate, e.Location, e.Description,
        e.OrganiserId, e.Organiser.FullName, e.DistanceKm, e.EventType, e.BannerImageUrl);

    /// <summary>
    /// Lists events. Public — no login required. Optional filters: free-text search,
    /// event type (Run/Walk/Cycle), location, a date range, or upcoming events only.
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetEvents(
        [FromQuery] string? search,
        [FromQuery] string? eventType,
        [FromQuery] string? location,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] bool upcomingOnly = false)
    {
        IQueryable<Event> query = _context.Events;

        if (upcomingOnly)
        {
            var today = DateTime.UtcNow.Date;
            query = query.Where(e => e.EventDate >= today);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(e => e.EventName.Contains(term) || (e.Description != null && e.Description.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(eventType))
        {
            var type = EventTypes.FirstOrDefault(t => t.Equals(eventType.Trim(), StringComparison.OrdinalIgnoreCase));
            if (type is null) return BadRequest("Event type must be Run, Walk or Cycle.");
            query = query.Where(e => e.EventType == type);
        }

        if (!string.IsNullOrWhiteSpace(location))
        {
            var place = location.Trim();
            query = query.Where(e => e.Location.Contains(place));
        }

        if (from.HasValue)
        {
            var start = from.Value.Date;
            query = query.Where(e => e.EventDate >= start);
        }

        if (to.HasValue)
        {
            var endExclusive = to.Value.Date.AddDays(1);
            query = query.Where(e => e.EventDate < endExclusive);
        }

        var events = await query.OrderBy(e => e.EventDate).Select(ToResponse).ToListAsync();
        return Ok(events);
    }

    /// <summary>Returns the logged-in organiser's own events with enrolment totals (dashboard data).</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> GetMyEvents()
    {
        var organiserId = CurrentUserId;
        var today = DateTime.UtcNow.Date;

        var rows = await _context.Events
            .Where(e => e.OrganiserId == organiserId)
            .OrderBy(e => e.EventDate)
            .Select(e => new
            {
                e.EventId,
                e.EventName,
                e.EventDate,
                e.Location,
                e.EventType,
                e.DistanceKm,
                e.BannerImageUrl,
                Enrolments = e.Enrolments.Count(en => en.Status != "Cancelled"),
                Categories = e.Categories.Count()
            })
            .ToListAsync();

        var summaries = rows.Select(r => new EventSummaryResponse(
            r.EventId, r.EventName, r.EventDate, r.Location, r.EventType,
            r.DistanceKm, r.Enrolments, r.Categories, r.EventDate >= today, r.BannerImageUrl));

        return Ok(summaries);
    }

    /// <summary>Returns a single event's details. Public — no login required.</summary>
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetEvent(int id)
    {
        var ev = await _context.Events
            .Where(e => e.EventId == id)
            .Select(ToResponse)
            .FirstOrDefaultAsync();

        if (ev is null) return NotFound();

        return Ok(ev);
    }

    /// <summary>Creates a new event owned by the logged-in organiser.</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> CreateEvent(CreateEventRequest request)
    {
        var error = Validate(request.EventName, request.Location, request.DistanceKm, request.EventType, out var type);
        if (error is not null) return BadRequest(error);

        var ev = new Event
        {
            EventName = request.EventName.Trim(),
            EventDate = request.EventDate,
            Location = request.Location.Trim(),
            Description = request.Description,
            DistanceKm = request.DistanceKm,
            EventType = type,
            OrganiserId = CurrentUserId
        };

        _context.Events.Add(ev);
        await _context.SaveChangesAsync();

        var organiserName = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        return CreatedAtAction(nameof(GetEvent), new { id = ev.EventId },
            new EventResponse(ev.EventId, ev.EventName, ev.EventDate, ev.Location, ev.Description,
                ev.OrganiserId, organiserName, ev.DistanceKm, ev.EventType, ev.BannerImageUrl));
    }

    /// <summary>Updates an event. Only the organiser who created it may update it.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UpdateEvent(int id, UpdateEventRequest request)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var error = Validate(request.EventName, request.Location, request.DistanceKm, request.EventType, out var type);
        if (error is not null) return BadRequest(error);

        ev.EventName = request.EventName.Trim();
        ev.EventDate = request.EventDate;
        ev.Location = request.Location.Trim();
        ev.Description = request.Description;
        ev.DistanceKm = request.DistanceKm;
        ev.EventType = type;

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

        var hasEnrolments = await _context.Enrolments.AnyAsync(e => e.EventId == id);
        if (hasEnrolments)
            return Conflict("This event already has enrolments and cannot be deleted.");

        _context.Events.Remove(ev);
        await _context.SaveChangesAsync();
        return NoContent();
    }

    /// <summary>Uploads (or replaces) an event's banner image in Azure Blob Storage.</summary>
    [HttpPost("{id:int}/banner")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> UploadBanner(int id, IFormFile file)
    {
        var ev = await _context.Events.FindAsync(id);
        if (ev is null) return NotFound();
        if (ev.OrganiserId != CurrentUserId) return Forbid();

        var validationError = ImageValidator.Validate(file);
        if (validationError is not null) return BadRequest(validationError);

        if (!_blobStorage.IsConfigured)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, "Image storage is not configured on the server.");

        try
        {
            var url = await _blobStorage.UploadImageAsync(file, "event-banners");
            ev.BannerImageUrl = url;
            await _context.SaveChangesAsync();
            return Ok(new ImageUploadResponse(url));
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(ex, "Azure Blob Storage upload failed for event {EventId}", id);
            return StatusCode(StatusCodes.Status502BadGateway,
                "The image could not be saved to Azure Blob Storage. Please try again later.");
        }
    }

    private static string? Validate(string? name, string? location, decimal distanceKm, string? eventType, out string normalisedType)
    {
        normalisedType = EventTypes.FirstOrDefault(t => t.Equals(eventType?.Trim(), StringComparison.OrdinalIgnoreCase)) ?? string.Empty;

        if (string.IsNullOrWhiteSpace(name)) return "Event name is required.";
        if (string.IsNullOrWhiteSpace(location)) return "Location is required.";
        if (distanceKm <= 0) return "Distance must be greater than 0 km.";
        if (normalisedType.Length == 0) return "Event type must be Run, Walk or Cycle.";

        return null;
    }
}
