namespace RaceDay.Api.DTOs;

public record CreateEventRequest(string EventName, DateTime EventDate, string Location, string? Description);
public record UpdateEventRequest(string EventName, DateTime EventDate, string Location, string? Description);
public record EventResponse(int EventId, string EventName, DateTime EventDate, string Location, string? Description, int OrganiserId, string OrganiserName);
