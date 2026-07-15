using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Domain.Sessions;

namespace Nevma.Identity.Api.Application.Sessions;

public sealed class DeviceSessionService(
    IDeviceSessionRepository repository,
    ISessionTokenRevoker tokenRevoker,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<DeviceSession> StartAsync(
        Guid id,
        Guid userId,
        string deviceId,
        string deviceName,
        string platform,
        string authorizationId,
        CancellationToken cancellationToken = default)
    {
        var now = timeProvider.GetUtcNow();
        var previous = await repository.ListActiveByDeviceAsync(userId, deviceId, cancellationToken);
        foreach (var session in previous)
            session.Revoke(now);

        var current = DeviceSession.Create(
            id,
            userId,
            deviceId,
            deviceName,
            platform,
            authorizationId,
            now);
        await repository.AddAsync(current, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        foreach (var session in previous)
            await tokenRevoker.RevokeAsync(session.AuthorizationId, cancellationToken);

        return current;
    }

    public async Task<IReadOnlyList<DeviceSessionResponse>> ListAsync(
        Guid userId,
        Guid? currentSessionId,
        CancellationToken cancellationToken = default)
    {
        var sessions = await repository.ListAsync(userId, cancellationToken);
        return sessions.Select(session => ToResponse(session, session.Id == currentSessionId)).ToArray();
    }

    public async Task<bool> RevokeAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var session = await repository.GetAsync(id, userId, cancellationToken);
        if (session is null || session.RevokedAt is not null)
            return false;

        session.Revoke(timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await tokenRevoker.RevokeAsync(session.AuthorizationId, cancellationToken);
        return true;
    }

    private static DeviceSessionResponse ToResponse(DeviceSession session, bool isCurrent) =>
        new(
            session.Id,
            session.DeviceId,
            session.DeviceName,
            session.Platform,
            session.CreatedAt,
            session.LastSeenAt,
            session.RevokedAt,
            isCurrent);
}
