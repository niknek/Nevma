using Nevma.Contracts.Integration;

namespace Nevma.Messaging.Api.Application.Integration;

public interface IUserRealtimePublisher
{
    Task PublishMeetingInvitationChangedAsync(
        IReadOnlyCollection<Guid> userIds,
        MeetingInvitationChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default);
}
