using Nevma.Identity.Api.Domain.Sessions;

namespace Nevma.Identity.Api.Application.Sessions;

public interface IDeviceSessionRepository
{
    Task AddAsync(DeviceSession session, CancellationToken cancellationToken = default);
    Task<DeviceSession?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceSession>> ListAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DeviceSession>> ListActiveByDeviceAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default);
}

public interface ISessionTokenRevoker
{
    Task RevokeAsync(string authorizationId, CancellationToken cancellationToken = default);
}
