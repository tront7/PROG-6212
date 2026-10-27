namespace RaceDay.Web.Models;

// Shapes of the JSON the RaceDay API returns. These mirror the API's response records;
// the MVC app deliberately does not reference the API project (it has no database access).

public record AuthDto(int UserId, string FullName, string Email, string Role, string? ProfilePictureUrl);

public record UserProfileDto(
    int UserId, string FullName, string Email, string? PhoneNumber, string Role, DateTime CreatedAt, string? ProfilePictureUrl);

public record ImageUploadDto(string Url);

public record EventDto(
    int EventId, string EventName, DateTime EventDate, string Location, string? Description,
    int OrganiserId, string OrganiserName, decimal DistanceKm, string EventType, string? BannerImageUrl);

public record EventSummaryDto(
    int EventId, string EventName, DateTime EventDate, string Location, string EventType,
    decimal DistanceKm, int TotalEnrolments, int CategoryCount, bool IsUpcoming, string? BannerImageUrl);

public record CategoryDto(
    int CategoryId, int EventId, string CategoryName, decimal DistanceKm, int MaxParticipants, decimal Price, int EnrolledCount)
{
    public int SpotsLeft => Math.Max(0, MaxParticipants - EnrolledCount);
    public bool IsFull => EnrolledCount >= MaxParticipants;
}

public record EnrolmentDto(
    int EnrolmentId, int UserId, int EventId, int CategoryId, DateTime EnrolmentDate, string Status,
    string EventName, DateTime EventDate, string CategoryName,
    string ParticipantName, string ParticipantEmail,
    int? ResultId, TimeSpan? FinishTime, int? Position, string? ResultStatus);

public record MyResultDto(
    int ResultId, int EnrolmentId, int EventId, string EventName, DateTime EventDate, string CategoryName,
    TimeSpan? FinishTime, int? Position, int TotalFinishers, string Status, DateTime RecordedAt);
