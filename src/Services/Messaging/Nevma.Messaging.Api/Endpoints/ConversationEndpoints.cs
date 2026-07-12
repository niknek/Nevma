using Microsoft.AspNetCore.SignalR;
using Nevma.Contracts.Messaging;
using Nevma.Messaging.Api.Application.Messages;
using Nevma.Messaging.Api.Realtime;

namespace Nevma.Messaging.Api.Endpoints;

public static class ConversationEndpoints
{
    public static IEndpointRouteBuilder MapConversationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var conversations = endpoints.MapGroup("/api/conversations")
            .WithTags("Conversations");

        conversations.MapGet("/{conversationId:guid}/messages", (
            Guid conversationId,
            MessageService service) =>
            Results.Ok(service.GetMessages(conversationId)));

        conversations.MapPost("/{conversationId:guid}/messages", async (
            Guid conversationId,
            SendMessageRequest request,
            MessageService service,
            IHubContext<ChatHub> hub,
            CancellationToken cancellationToken) =>
        {
            var result = service.Send(conversationId, request);
            if (!result.IsSuccess)
                return Results.ValidationProblem(result.Errors);

            var message = result.Message!;
            await hub.Clients.Group(ChatHub.GroupName(conversationId))
                .SendAsync("message.received", message, cancellationToken);

            return Results.Created(
                $"/api/conversations/{conversationId}/messages/{message.Id}",
                message);
        });

        return endpoints;
    }
}
