namespace RaceDay.Api.DTOs;

public record CreateEnrolmentRequest(int EventId, int CategoryId);
public record EnrolmentResponse(int EnrolmentId, int UserId, int EventId, int CategoryId, DateTime EnrolmentDate, string Status);
