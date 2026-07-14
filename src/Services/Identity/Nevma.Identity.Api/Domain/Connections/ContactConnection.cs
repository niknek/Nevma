namespace Nevma.Identity.Api.Domain.Connections;

public sealed class ContactConnection
{
    private ContactConnection(
        Guid id,
        Guid requesterId,
        Guid addresseeId,
        ContactConnectionStatus status,
        DateTimeOffset createdAt,
        DateTimeOffset? respondedAt)
    {
        Id = id;
        RequesterId = requesterId;
        AddresseeId = addresseeId;
        PairFirstUserId = Min(requesterId, addresseeId);
        PairSecondUserId = Max(requesterId, addresseeId);
        Status = status;
        CreatedAt = createdAt;
        RespondedAt = respondedAt;
    }

    public Guid Id { get; }
    public Guid RequesterId { get; private set; }
    public Guid AddresseeId { get; private set; }
    public Guid PairFirstUserId { get; }
    public Guid PairSecondUserId { get; }
    public ContactConnectionStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? RespondedAt { get; private set; }

    public static ContactConnection Create(Guid requesterId, Guid addresseeId, DateTimeOffset createdAt)
    {
        if (requesterId == addresseeId)
            throw new ArgumentException("A user cannot connect to themselves.", nameof(addresseeId));

        return new ContactConnection(
            Guid.NewGuid(),
            requesterId,
            addresseeId,
            ContactConnectionStatus.Pending,
            createdAt,
            null);
    }

    public ConnectionTransition Accept(Guid actorId, DateTimeOffset respondedAt) =>
        Decide(actorId, ContactConnectionStatus.Accepted, respondedAt);

    public ConnectionTransition Reject(Guid actorId, DateTimeOffset respondedAt) =>
        Decide(actorId, ContactConnectionStatus.Rejected, respondedAt);

    public bool Reopen(Guid requesterId, Guid addresseeId, DateTimeOffset createdAt)
    {
        if (Status != ContactConnectionStatus.Rejected)
            return false;

        if (PairFirstUserId != Min(requesterId, addresseeId) ||
            PairSecondUserId != Max(requesterId, addresseeId))
        {
            return false;
        }

        RequesterId = requesterId;
        AddresseeId = addresseeId;
        Status = ContactConnectionStatus.Pending;
        CreatedAt = createdAt;
        RespondedAt = null;
        return true;
    }

    private ConnectionTransition Decide(
        Guid actorId,
        ContactConnectionStatus nextStatus,
        DateTimeOffset respondedAt)
    {
        if (actorId != AddresseeId)
            return ConnectionTransition.Forbidden;

        if (Status != ContactConnectionStatus.Pending)
            return ConnectionTransition.AlreadyHandled;

        Status = nextStatus;
        RespondedAt = respondedAt;
        return ConnectionTransition.Success;
    }

    private static Guid Min(Guid first, Guid second) => first.CompareTo(second) <= 0 ? first : second;

    private static Guid Max(Guid first, Guid second) => first.CompareTo(second) >= 0 ? first : second;
}

public enum ContactConnectionStatus
{
    Pending,
    Accepted,
    Rejected
}

public enum ConnectionTransition
{
    Success,
    Forbidden,
    AlreadyHandled
}
