using System.Collections.Concurrent;
using Nevma.Contracts.Messaging;
using Nevma.Messaging.Api.Application.Presence;

namespace Nevma.Messaging.Api.Infrastructure.Realtime;

public sealed class InMemoryUserPresenceTracker(TimeProvider timeProvider) : IUserPresenceTracker
{
    private readonly ConcurrentDictionary<Guid, PresenceState> _states = new();

    public PresenceResponse Connected(Guid userId)
    {
        var state = _states.AddOrUpdate(
            userId,
            _ => new PresenceState(1, null),
            (_, current) => current with { Connections = current.Connections + 1 });
        return ToResponse(userId, state);
    }

    public PresenceResponse Disconnected(Guid userId)
    {
        var now = timeProvider.GetUtcNow();
        var state = _states.AddOrUpdate(
            userId,
            _ => new PresenceState(0, now),
            (_, current) => new PresenceState(Math.Max(0, current.Connections - 1), now));
        return ToResponse(userId, state);
    }

    public PresenceResponse Get(Guid userId) =>
        _states.TryGetValue(userId, out var state)
            ? ToResponse(userId, state)
            : new PresenceResponse(userId, false, null);

    private static PresenceResponse ToResponse(Guid userId, PresenceState state) =>
        new(userId, state.Connections > 0, state.Connections > 0 ? null : state.LastSeenAt);

    private sealed record PresenceState(int Connections, DateTimeOffset? LastSeenAt);
}
