using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using Google.Apis.Auth.OAuth2;
using Microsoft.Extensions.Options;
using Nevma.Notifications.Api.Application.Delivery;
using FirebaseNotification = FirebaseAdmin.Messaging.Notification;

namespace Nevma.Notifications.Api.Infrastructure.Delivery;

public sealed class FirebasePushNotificationSender : IPushNotificationSender, IDisposable
{
    private readonly FirebaseApp _app;

    public FirebasePushNotificationSender(IOptions<PushDeliveryOptions> options)
    {
        var projectId = options.Value.ProjectId;
        if (string.IsNullOrWhiteSpace(projectId))
            throw new InvalidOperationException("PushDelivery:ProjectId is required when push delivery is enabled.");
        _app = FirebaseApp.Create(
            new AppOptions
            {
                Credential = GoogleCredential.GetApplicationDefault(),
                ProjectId = projectId
            },
            "nevma-notifications");
    }

    public async Task<PushSendResult> SendAsync(
        string target,
        string title,
        string body,
        IReadOnlyDictionary<string, string> data,
        CancellationToken cancellationToken = default)
    {
        var message = new Message
        {
#pragma warning disable CS0618
            Token = target,
#pragma warning restore CS0618
            Notification = new FirebaseNotification
            {
                Title = title,
                Body = body
            },
            Data = new Dictionary<string, string>(data)
        };

        try
        {
            var providerMessageId = await FirebaseMessaging.GetMessaging(_app)
                .SendAsync(message, dryRun: false, cancellationToken);
            return new PushSendResult.Sent(providerMessageId);
        }
        catch (FirebaseMessagingException exception) when (
            exception.MessagingErrorCode is
                MessagingErrorCode.Unregistered or
                MessagingErrorCode.InvalidArgument or
                MessagingErrorCode.SenderIdMismatch)
        {
            return new PushSendResult.PermanentFailure(
                exception.MessagingErrorCode?.ToString() ?? "PermanentFailure");
        }
        catch (FirebaseMessagingException exception)
        {
            return new PushSendResult.RetryableFailure(
                exception.MessagingErrorCode?.ToString() ?? exception.ErrorCode.ToString());
        }
    }

    public void Dispose() => _app.Delete();
}
