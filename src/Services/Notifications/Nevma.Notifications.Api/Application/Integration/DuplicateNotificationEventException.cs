namespace Nevma.Notifications.Api.Application.Integration;

public sealed class DuplicateNotificationEventException(Exception innerException)
    : Exception("The notification integration event was already processed.", innerException);
