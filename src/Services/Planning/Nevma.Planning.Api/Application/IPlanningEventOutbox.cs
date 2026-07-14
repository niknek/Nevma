using Nevma.Contracts.Integration;

namespace Nevma.Planning.Api.Application;

public interface IPlanningEventOutbox
{
    void Add(MeetingInvitationChangedIntegrationEvent integrationEvent);
}
