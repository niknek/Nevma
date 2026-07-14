namespace Nevma.Identity.Api.Domain.Users;

public sealed class UserReport
{
    private UserReport(
        Guid id,
        Guid reporterId,
        Guid reportedUserId,
        string reason,
        string? details,
        DateTimeOffset createdAt)
    {
        Id = id;
        ReporterId = reporterId;
        ReportedUserId = reportedUserId;
        Reason = reason;
        Details = details;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public Guid ReporterId { get; }
    public Guid ReportedUserId { get; }
    public string Reason { get; }
    public string? Details { get; }
    public DateTimeOffset CreatedAt { get; }

    public static UserReport Create(
        Guid reporterId,
        Guid reportedUserId,
        string reason,
        string? details,
        DateTimeOffset now) =>
        new(Guid.NewGuid(), reporterId, reportedUserId, reason.Trim(), details?.Trim(), now);
}
