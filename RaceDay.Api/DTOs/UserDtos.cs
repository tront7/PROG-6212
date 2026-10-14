namespace RaceDay.Api.DTOs;

public record UserProfileResponse(int UserId, string FullName, string Email, string? PhoneNumber, string Role, DateTime CreatedAt);
public record UpdateUserProfileRequest(string FullName, string? PhoneNumber, string Email);
