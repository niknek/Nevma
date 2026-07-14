using Microsoft.EntityFrameworkCore;
using Nevma.Messaging.Api.Application.Conversations;
using Nevma.Messaging.Api.Domain.Conversations;
using Nevma.Messaging.Api.Infrastructure.Persistence;

namespace Nevma.Messaging.Api.Infrastructure.Conversations;

public sealed class EfConversationRepository(MessagingDbContext dbContext) : IConversationRepository
{
    public async Task AddAsync(
        Conversation conversation,
        CancellationToken cancellationToken = default) =>
        await dbContext.Conversations.AddAsync(conversation, cancellationToken);

    public void Discard(Conversation conversation)
    {
        foreach (var participant in conversation.Participants)
            dbContext.Entry(participant).State = EntityState.Detached;
        dbContext.Entry(conversation).State = EntityState.Detached;
    }

    public Task<Conversation?> FindPersonalAsync(
        string personalKey,
        CancellationToken cancellationToken = default) =>
        dbContext.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Participants)
            .SingleOrDefaultAsync(
                conversation => conversation.PersonalKey == personalKey,
                cancellationToken);

    public async Task<IReadOnlyList<Conversation>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Conversations
            .AsNoTracking()
            .Include(conversation => conversation.Participants)
            .Where(conversation => conversation.Participants.Any(participant => participant.UserId == userId))
            .OrderByDescending(conversation => conversation.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<bool> IsParticipantAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.ConversationParticipants.AnyAsync(
            participant => participant.ConversationId == conversationId && participant.UserId == userId,
            cancellationToken);
}
