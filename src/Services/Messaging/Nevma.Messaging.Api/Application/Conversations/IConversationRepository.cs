using Nevma.Messaging.Api.Domain.Conversations;

namespace Nevma.Messaging.Api.Application.Conversations;

public interface IConversationRepository
{
    Task AddAsync(Conversation conversation, CancellationToken cancellationToken = default);
    void Discard(Conversation conversation);
    Task<Conversation?> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Conversation?> FindPersonalAsync(string personalKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Conversation>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> IsParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default);
    Task<bool> ShareConversationAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken = default);
}
