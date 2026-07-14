using Microsoft.AspNetCore.SignalR;
using Nevma.Contracts.Integration;
using Nevma.Messaging.Api.Application.Integration;
using Nevma.Messaging.Api.Realtime;

namespace Nevma.Messaging.Api.Infrastructure.Realtime;

public sealed class SignalRUserRealtimePublisher(IHubContext<ChatHub> hubContext)
    : IUserRealtimePublisher
{
    public Task PublishMeetingInvitationChangedAsync(
        IReadOnlyCollection<Guid> userIds,
        MeetingInvitationChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default) =>
        hubContext.Clients.Groups(
                userIds.Distinct().Select(ChatHub.UserGroupName).ToArray())
            .SendAsync(
                "meeting-invitation.changed",
                integrationEvent,
                cancellationToken);
}
