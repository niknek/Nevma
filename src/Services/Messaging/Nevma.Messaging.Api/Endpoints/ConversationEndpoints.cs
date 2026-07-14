using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;
using Nevma.Contracts.Messaging;
using Nevma.Messaging.Api.Application.Conversations;
using Nevma.Messaging.Api.Application.Messages;
using Nevma.Messaging.Api.Realtime;

namespace Nevma.Messaging.Api.Endpoints;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var conversations = endpoints.MapGroup("/api/conversations")
            .RequireAuthorization()
            .WithTags("Conversations");

        conversations.MapPost("/", async (
            CreateConversationRequest request,
            ClaimsPrincipal principal,
            ConversationService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var creatorId))
                return Results.Unauthorized();

            var result = await service.CreateAsync(creatorId, request, cancellationToken);
            if (!result.IsSuccess)
                return Results.ValidationProblem(result.Errors);
            return result.IsNew
                ? Results.Created($"/api/conversations/{result.Conversation!.Id}", result.Conversation)
                : Results.Ok(result.Conversation);
        });

        conversations.MapGet("/", async (
            ClaimsPrincipal principal,
            ConversationService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();
            return Results.Ok(await service.ListAsync(userId, cancellationToken));
        });

        conversations.MapGet("/{conversationId:guid}/messages", async (
            Guid conversationId,
            long? before,
            int? take,
            ClaimsPrincipal principal,
            MessageService service,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var userId))
                return Results.Unauthorized();

            var page = await service.GetMessagesAsync(
                conversationId,
                userId,
                before,
                take ?? 50,
                cancellationToken);
            return page is null ? Results.NotFound() : Results.Ok(page);
        });

        conversations.MapPost("/{conversationId:guid}/messages", async (
            Guid conversationId,
            SendMessageRequest request,
            ClaimsPrincipal principal,
            MessageService service,
            IHubContext<ChatHub> hub,
            CancellationToken cancellationToken) =>
        {
            if (!TryGetUserId(principal, out var senderId))
                return Results.Unauthorized();

            var result = await service.SendAsync(
                conversationId,
                senderId,
                request,
                cancellationToken);
            if (result is SendMessageResult.NotFound)
                return Results.NotFound();
            if (result is SendMessageResult.Invalid invalid)
                return Results.ValidationProblem(
                    new Dictionary<string, string[]> { [invalid.Field] = [invalid.Error] });

            var message = ((SendMessageResult.Sent)result).Message;
            await hub.Clients.Group(ChatHub.ConversationGroupName(conversationId))
                .SendAsync("message.received", message, cancellationToken);
            return Results.Created(
                $"/api/conversations/{conversationId}/messages/{message.Id}",
                message);
        });

        return endpoints;
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue("sub"), out userId);
}
