namespace Nevma.Notifications.Api.Domain.Delivery;

public sealed class DeliveryAttempt
{
    private DeliveryAttempt(
        Guid id,
        Guid notificationId,
        Guid pushDeviceId,
        DateTimeOffset createdAt)
    {
        Id = id;
        NotificationId = notificationId;
        PushDeviceId = pushDeviceId;
        CreatedAt = createdAt;
        NextAttemptAt = createdAt;
    }

    public Guid Id { get; }
    public Guid NotificationId { get; }
    public Guid PushDeviceId { get; }
    public DeliveryStatus Status { get; private set; } = DeliveryStatus.Pending;
    public int Attempts { get; private set; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset NextAttemptAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? ProviderMessageId { get; private set; }
    public string? LastError { get; private set; }
    public Guid? LockId { get; private set; }
    public DateTimeOffset? LockedUntil { get; private set; }

    public static DeliveryAttempt Create(
        Guid notificationId,
        Guid pushDeviceId,
        DateTimeOffset createdAt) =>
        new(Guid.NewGuid(), notificationId, pushDeviceId, createdAt);

    public void MarkSent(string providerMessageId, DateTimeOffset completedAt)
    {
        Attempts++;
        Status = DeliveryStatus.Sent;
        ProviderMessageId = providerMessageId;
        CompletedAt = completedAt;
        LastError = null;
        ClearLock();
    }

    public void MarkPermanentFailure(string error, DateTimeOffset completedAt)
    {
        Attempts++;
        Status = DeliveryStatus.PermanentFailure;
        LastError = Limit(error);
        CompletedAt = completedAt;
        ClearLock();
    }

    public void MarkRetryableFailure(string error, DateTimeOffset failedAt)
    {
        Attempts++;
        var delaySeconds = Math.Min(Math.Pow(2, Attempts), 15 * 60);
        NextAttemptAt = failedAt.AddSeconds(delaySeconds);
        LastError = Limit(error);
        ClearLock();
    }

    private void ClearLock()
    {
        LockId = null;
        LockedUntil = null;
    }

    private static string Limit(string error) =>
        error.Length <= 2_000 ? error : error[..2_000];
}

public enum DeliveryStatus
{
    Pending,
    Sent,
    PermanentFailure
}
