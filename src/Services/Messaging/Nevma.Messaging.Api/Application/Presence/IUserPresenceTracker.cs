using Nevma.Contracts.Messaging;

namespace Nevma.Messaging.Api.Application.Presence;

public interface IUserPresenceTracker
{
    Task<PresenceResponse> ConnectedAsync(Guid userId);
    Task<PresenceResponse> DisconnectedAsync(Guid userId);
    Task<PresenceResponse> RefreshAsync(Guid userId);
    Task<PresenceResponse> GetAsync(Guid userId);
}
