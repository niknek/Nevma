using Nevma.Contracts.Messaging;

namespace Nevma.Messaging.Api.Application.Presence;

public interface IUserPresenceTracker
{
    PresenceResponse Connected(Guid userId);
    PresenceResponse Disconnected(Guid userId);
    PresenceResponse Get(Guid userId);
}
