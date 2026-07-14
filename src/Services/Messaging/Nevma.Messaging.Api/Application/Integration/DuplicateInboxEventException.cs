namespace Nevma.Messaging.Api.Application.Integration;

public sealed class DuplicateInboxEventException(Exception innerException)
    : Exception("The integration event was already processed.", innerException);
