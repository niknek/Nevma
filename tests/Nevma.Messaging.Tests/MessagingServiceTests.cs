using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Messaging;
using Nevma.Messaging.Api.Application.Conversations;
using Nevma.Messaging.Api.Application.Messages;
using Nevma.Messaging.Api.Domain.Messages;
using Nevma.Messaging.Api.Infrastructure.Conversations;
using Nevma.Messaging.Api.Infrastructure.Messages;
using Nevma.Messaging.Api.Infrastructure.Persistence;

namespace Nevma.Messaging.Tests;

public sealed class MessagingServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Authenticated_user_is_added_to_the_conversation()
    {
        await using var context = CreateContext();
        var service = CreateConversationService(context);
        var creatorId = Guid.NewGuid();
        var otherId = Guid.NewGuid();

        var result = await service.CreateAsync(
            creatorId,
            PersonalConversation(otherId));

        Assert.True(result.IsSuccess);
        Assert.True(result.IsNew);
        Assert.Equal(new[] { creatorId, otherId }.Order(), result.Conversation!.ParticipantIds.Order());
        Assert.Equal(2, await context.ConversationParticipants.CountAsync());
    }

    [Fact]
    public async Task Personal_conversation_is_reused_for_the_same_pair()
    {
        await using var context = CreateContext();
        var service = CreateConversationService(context);
        var firstUser = Guid.NewGuid();
        var secondUser = Guid.NewGuid();

        var first = await service.CreateAsync(firstUser, PersonalConversation(secondUser));
        var repeated = await service.CreateAsync(secondUser, PersonalConversation(firstUser));

        Assert.True(first.IsNew);
        Assert.False(repeated.IsNew);
        Assert.Equal(first.Conversation!.Id, repeated.Conversation!.Id);
        Assert.Equal(1, await context.Conversations.CountAsync());
    }

    [Fact]
    public async Task Non_member_cannot_read_or_send_messages()
    {
        await using var context = CreateContext();
        var conversationService = CreateConversationService(context);
        var messageService = CreateMessageService(context);
        var conversation = await conversationService.CreateAsync(
            Guid.NewGuid(),
            PersonalConversation(Guid.NewGuid()));
        var strangerId = Guid.NewGuid();

        var page = await messageService.GetMessagesAsync(
            conversation.Conversation!.Id,
            strangerId,
            null,
            50);
        var send = await messageService.SendAsync(
            conversation.Conversation.Id,
            strangerId,
            new SendMessageRequest("This should not be stored"));

        Assert.Null(page);
        Assert.IsType<SendMessageResult.NotFound>(send);
        Assert.Empty(context.Messages);
    }

    [Fact]
    public async Task Message_sender_comes_from_the_authenticated_user()
    {
        await using var context = CreateContext();
        var conversationService = CreateConversationService(context);
        var messageService = CreateMessageService(context);
        var senderId = Guid.NewGuid();
        var conversation = await conversationService.CreateAsync(
            senderId,
            PersonalConversation(Guid.NewGuid()));

        var result = await messageService.SendAsync(
            conversation.Conversation!.Id,
            senderId,
            new SendMessageRequest("  Hello  "));

        var sent = Assert.IsType<SendMessageResult.Sent>(result);
        Assert.Equal(senderId, sent.Message.SenderId);
        Assert.Equal("Hello", sent.Message.Text);
        Assert.Equal(senderId, (await context.Messages.SingleAsync()).SenderId);
    }

    [Fact]
    public async Task Message_pages_use_a_stable_sequence_cursor()
    {
        await using var context = CreateContext();
        var conversationService = CreateConversationService(context);
        var messageService = CreateMessageService(context);
        var senderId = Guid.NewGuid();
        var conversation = await conversationService.CreateAsync(
            senderId,
            PersonalConversation(Guid.NewGuid()));
        await AddMessageAsync(context, conversation.Conversation!.Id, senderId, "First", 1);
        await AddMessageAsync(context, conversation.Conversation.Id, senderId, "Second", 2);
        await AddMessageAsync(context, conversation.Conversation.Id, senderId, "Third", 3);

        var firstPage = await messageService.GetMessagesAsync(
            conversation.Conversation.Id,
            senderId,
            null,
            2);
        var secondPage = await messageService.GetMessagesAsync(
            conversation.Conversation.Id,
            senderId,
            firstPage!.NextBefore,
            2);

        Assert.Equal(["Third", "Second"], firstPage.Items.Select(message => message.Text));
        Assert.NotNull(firstPage.NextBefore);
        Assert.Collection(secondPage!.Items, message => Assert.Equal("First", message.Text));
        Assert.Null(secondPage.NextBefore);
    }

    [Fact]
    public async Task Group_conversation_requires_a_title_and_three_members()
    {
        await using var context = CreateContext();
        var service = CreateConversationService(context);

        var result = await service.CreateAsync(
            Guid.NewGuid(),
            new CreateConversationRequest(
                ConversationKind.Group,
                null,
                [Guid.NewGuid()]));

        Assert.False(result.IsSuccess);
        Assert.Contains(nameof(CreateConversationRequest.Title), result.Errors.Keys);
        Assert.Contains(nameof(CreateConversationRequest.ParticipantIds), result.Errors.Keys);
    }

    private static ConversationService CreateConversationService(MessagingDbContext context) =>
        new(new EfConversationRepository(context), context, new FixedTimeProvider(Now));

    private static MessageService CreateMessageService(MessagingDbContext context) =>
        new(
            new EfMessageRepository(context),
            new EfConversationRepository(context),
            context,
            new FixedTimeProvider(Now));

    private static MessagingDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<MessagingDbContext>()
            .UseInMemoryDatabase($"messaging-{Guid.NewGuid():N}")
            .Options;
        return new MessagingDbContext(options);
    }

    private static CreateConversationRequest PersonalConversation(Guid otherUserId) =>
        new(ConversationKind.Personal, null, [otherUserId]);

    private static async Task AddMessageAsync(
        MessagingDbContext context,
        Guid conversationId,
        Guid senderId,
        string text,
        long sequence)
    {
        var message = Message.Create(conversationId, senderId, text, Now);
        context.Messages.Add(message);
        context.Entry(message).Property(item => item.Sequence).CurrentValue = sequence;
        await context.SaveChangesAsync();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
