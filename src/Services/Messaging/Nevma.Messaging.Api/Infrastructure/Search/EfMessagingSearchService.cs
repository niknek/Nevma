using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Search;
using Nevma.Messaging.Api.Application.Search;
using Nevma.Messaging.Api.Infrastructure.Persistence;

namespace Nevma.Messaging.Api.Infrastructure.Search;

public sealed class EfMessagingSearchService(MessagingDbContext dbContext) : IMessagingSearchService
{
    public async Task<IReadOnlyList<SearchResultItem>> SearchAsync(
        Guid userId,
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim().ToLowerInvariant();
        var take = Math.Clamp(limit, 1, 50);
        var conversations = await dbContext.Conversations
            .AsNoTracking()
            .Where(item => item.Title != null && item.Title.ToLower().Contains(normalized) &&
                dbContext.ConversationParticipants.Any(participant =>
                    participant.ConversationId == item.Id && participant.UserId == userId))
            .OrderByDescending(item => item.CreatedAt)
            .Take(take)
            .Select(item => new { item.Id, item.Title, item.CreatedAt })
            .ToArrayAsync(cancellationToken);
        var messages = await (
            from message in dbContext.Messages.AsNoTracking()
            join conversation in dbContext.Conversations.AsNoTracking()
                on message.ConversationId equals conversation.Id
            where message.DeletedAt == null && message.Text.ToLower().Contains(normalized) &&
                dbContext.ConversationParticipants.Any(participant =>
                    participant.ConversationId == message.ConversationId && participant.UserId == userId)
            orderby message.SentAt descending
            select new
            {
                message.Id,
                message.ConversationId,
                ConversationTitle = conversation.Title,
                message.Text,
                message.SentAt
            })
            .Take(take)
            .ToArrayAsync(cancellationToken);

        return conversations.Select(item => new SearchResultItem(
                "messaging", "conversation", item.Id, null, item.Title!, null, item.CreatedAt))
            .Concat(messages.Select(item => new SearchResultItem(
                "messaging",
                "message",
                item.Id,
                item.ConversationId,
                item.ConversationTitle ?? "Conversation",
                Truncate(item.Text),
                item.SentAt)))
            .OrderByDescending(item => item.UpdatedAt)
            .Take(take)
            .ToArray();
    }

    private static string Truncate(string value) => value.Length <= 240 ? value : value[..240];
}
