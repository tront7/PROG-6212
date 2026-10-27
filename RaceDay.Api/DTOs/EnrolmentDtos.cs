namespace RaceDay.Api.DTOs;

public record CreateEnrolmentRequest(int EventId, int CategoryId);
public record UpdateEnrolmentStatusRequest(string Status);

/// <summary>
/// Enrolment with enough joined detail for the UI (event, category, participant
/// and any captured result) so the front end never needs extra look-ups.
/// </summary>
public record EnrolmentResponse(
    int EnrolmentId, int UserId, int EventId, int CategoryId, DateTime EnrolmentDate, string Status,
    string EventName, DateTime EventDate, string CategoryName,
    string ParticipantName, string ParticipantEmail,
    int? ResultId, TimeSpan? FinishTime, int? Position, string? ResultStatus);
