using Microsoft.EntityFrameworkCore;
using Nevma.Messaging.Api.Domain.Conversations;
using Nevma.Messaging.Api.Domain.Messages;
using Nevma.Messaging.Api.Infrastructure.Persistence;
using Nevma.Messaging.Api.Infrastructure.Search;

namespace Nevma.Messaging.Tests;

public sealed class MessagingSearchTests
{
    [Fact]
    public async Task Search_never_returns_messages_from_another_conversation()
    {
        await using var context = new MessagingDbContext(
            new DbContextOptionsBuilder<MessagingDbContext>()
                .UseInMemoryDatabase($"messaging-search-{Guid.NewGuid():N}")
                .Options);
        var userId = Guid.NewGuid();
        var visible = Conversation.Create(
            ConversationKind.Personal, null, userId, [Guid.NewGuid()], DateTimeOffset.UtcNow);
        var hidden = Conversation.Create(
            ConversationKind.Personal, null, Guid.NewGuid(), [Guid.NewGuid()], DateTimeOffset.UtcNow);
        context.Conversations.AddRange(visible, hidden);
        context.Messages.AddRange(
            Message.Create(visible.Id, userId, "roadmap for the launch", DateTimeOffset.UtcNow),
            Message.Create(hidden.Id, hidden.CreatedBy, "secret roadmap", DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();
        var service = new EfMessagingSearchService(context);

        var results = await service.SearchAsync(userId, "roadmap", 20);

        var item = Assert.Single(results);
        Assert.Equal(visible.Id, item.ParentId);
        Assert.Equal("message", item.Kind);
    }
}
