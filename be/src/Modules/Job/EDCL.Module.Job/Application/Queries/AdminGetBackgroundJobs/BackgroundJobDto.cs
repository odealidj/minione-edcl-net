namespace EDCL.Module.Job.Application.Queries.AdminGetBackgroundJobs;

public sealed record BackgroundJobDto(
    string JobId,
    string State,
    string MethodName,
    DateTime? EnqueueAt,
    DateTime? FailedAt,
    string? ExceptionMessage,
    string? ExceptionDetails
);
