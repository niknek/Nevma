namespace Nevma.Messaging.Api.Application.Conversations;

public sealed class DuplicatePersonalConversationException(Exception innerException)
    : Exception("A personal conversation already exists for these participants.", innerException);
