namespace RaceDay.Api.DTOs;

public record CreateResultRequest(int EnrolmentId, TimeSpan? FinishTime, int? Position, string Status);
public record UpdateResultRequest(TimeSpan? FinishTime, int? Position, string Status);
public record ResultResponse(int ResultId, int EnrolmentId, TimeSpan? FinishTime, int? Position, string Status, DateTime RecordedAt);

/// <summary>A participant's own race history entry, including how many finished in their category.</summary>
public record MyResultResponse(
    int ResultId, int EnrolmentId, int EventId, string EventName, DateTime EventDate, string CategoryName,
    TimeSpan? FinishTime, int? Position, int TotalFinishers, string Status, DateTime RecordedAt);

/// <summary>Public leaderboard row for an event.</summary>
public record EventResultResponse(
    int ResultId, int EnrolmentId, string ParticipantName, string CategoryName,
    TimeSpan? FinishTime, int? Position, string Status);
