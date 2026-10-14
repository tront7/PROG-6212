namespace RaceDay.Api.DTOs;

public record CreateCategoryRequest(string CategoryName, decimal DistanceKm, int MaxParticipants, decimal Price);
public record UpdateCategoryRequest(string CategoryName, decimal DistanceKm, int MaxParticipants, decimal Price);
public record CategoryResponse(int CategoryId, int EventId, string CategoryName, decimal DistanceKm, int MaxParticipants, decimal Price);
