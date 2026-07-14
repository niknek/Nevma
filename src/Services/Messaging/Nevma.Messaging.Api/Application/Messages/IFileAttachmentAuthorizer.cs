namespace Nevma.Messaging.Api.Application.Messages;

public interface IFileAttachmentAuthorizer
{
    Task<bool> AuthorizeAsync(
        IReadOnlyCollection<Guid> fileIds,
        Guid senderId,
        IReadOnlyCollection<Guid> participantIds,
        string accessToken,
        CancellationToken cancellationToken = default);
}
