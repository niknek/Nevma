namespace Nevma.Notifications.Api.Application.Delivery;

public interface IPushNotificationSender
{
    Task<PushSendResult> SendAsync(
        string target,
        string title,
        string body,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken = default);
}

public abstract record PushSendResult
{
    public sealed record Sent(string ProviderMessageId) : PushSendResult;
    public sealed record PermanentFailure(string ErrorCode) : PushSendResult;
    public sealed record RetryableFailure(string ErrorCode) : PushSendResult;
}
