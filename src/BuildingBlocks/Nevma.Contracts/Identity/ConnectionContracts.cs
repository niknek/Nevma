namespace Nevma.Contracts.Identity;

public sealed record CreateContactConnectionRequest(Guid TargetUserId);

public sealed record ContactConnectionResponse(
    Guid Id,
    Guid RequesterId,
    Guid AddresseeId,
    ContactConnectionStatus Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? RespondedAt);

public enum ContactConnectionStatus
{
    Pending,
    Accepted,
    Rejected
}
