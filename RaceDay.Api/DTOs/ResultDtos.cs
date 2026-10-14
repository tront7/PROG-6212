namespace RaceDay.Api.DTOs;

public record CreateResultRequest(int EnrolmentId, TimeSpan? FinishTime, int? Position, string Status);
public record UpdateResultRequest(TimeSpan? FinishTime, int? Position, string Status);
public record ResultResponse(int ResultId, int EnrolmentId, TimeSpan? FinishTime, int? Position, string Status, DateTime RecordedAt);
