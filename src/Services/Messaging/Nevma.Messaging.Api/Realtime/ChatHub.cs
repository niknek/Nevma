using Microsoft.AspNetCore.SignalR;

namespace Nevma.Messaging.Api.Realtime;

public sealed class ChatHub : Hub
{
    public Task JoinConversation(Guid conversationId) =>
        Groups.AddToGroupAsync(Context.ConnectionId, GroupName(conversationId));

    public Task LeaveConversation(Guid conversationId) =>
        Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(conversationId));

    public Task Typing(Guid conversationId, Guid userId) =>
        Clients.OthersInGroup(GroupName(conversationId))
            .SendAsync("typing.changed", new { userId });

    public static string GroupName(Guid conversationId) => $"conversation:{conversationId:N}";
}
