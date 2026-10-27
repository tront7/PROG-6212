namespace RaceDay.Api.DTOs;

// Request records use optional trailing parameters so older callers keep working.
public record CreateEventRequest(string EventName, DateTime EventDate, string Location, string? Description, decimal DistanceKm = 0, string EventType = "Run");
public record UpdateEventRequest(string EventName, DateTime EventDate, string Location, string? Description, decimal DistanceKm = 0, string EventType = "Run");

public record EventResponse(
    int EventId, string EventName, DateTime EventDate, string Location, string? Description,
    int OrganiserId, string OrganiserName, decimal DistanceKm, string EventType, string? BannerImageUrl);

/// <summary>Row on the Organiser dashboard.</summary>
public record EventSummaryResponse(
    int EventId, string EventName, DateTime EventDate, string Location, string EventType,
    decimal DistanceKm, int TotalEnrolments, int CategoryCount, bool IsUpcoming, string? BannerImageUrl);
