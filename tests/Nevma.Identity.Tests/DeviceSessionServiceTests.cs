using Nevma.Identity.Api.Application;
using Nevma.Identity.Api.Application.Sessions;
using Nevma.Identity.Api.Domain.Sessions;

namespace Nevma.Identity.Tests;

public sealed class DeviceSessionServiceTests
{
    [Fact]
    public async Task Starting_the_same_device_revokes_its_previous_authorization()
    {
        var repository = new FakeRepository();
        var revoker = new FakeRevoker();
        var service = new DeviceSessionService(
            repository,
            revoker,
            new FakeUnitOfWork(),
            TimeProvider.System);
        var userId = Guid.NewGuid();

        var first = await service.StartAsync(
            Guid.NewGuid(), userId, "phone-1", "Pixel", "Android", "authorization-1");
        var second = await service.StartAsync(
            Guid.NewGuid(), userId, "phone-1", "Pixel", "Android", "authorization-2");

        Assert.NotNull(first.RevokedAt);
        Assert.Null(second.RevokedAt);
        Assert.Contains("authorization-1", revoker.AuthorizationIds);
    }

    [Fact]
    public async Task User_cannot_revoke_another_users_session()
    {
        var repository = new FakeRepository();
        var revoker = new FakeRevoker();
        var service = new DeviceSessionService(
            repository,
            revoker,
            new FakeUnitOfWork(),
            TimeProvider.System);
        var ownerId = Guid.NewGuid();
        var session = await service.StartAsync(
            Guid.NewGuid(), ownerId, "phone-1", "Pixel", "Android", "authorization-1");

        var revoked = await service.RevokeAsync(session.Id, Guid.NewGuid());

        Assert.False(revoked);
        Assert.Null(session.RevokedAt);
        Assert.Empty(revoker.AuthorizationIds);
    }

    private sealed class FakeRepository : IDeviceSessionRepository
    {
        private readonly List<DeviceSession> sessions = [];

        public Task AddAsync(DeviceSession session, CancellationToken cancellationToken = default)
        {
            sessions.Add(session);
            return Task.CompletedTask;
        }

        public Task<DeviceSession?> GetAsync(
            Guid id,
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(sessions.SingleOrDefault(session =>
                session.Id == id && session.UserId == userId));

        public Task<IReadOnlyList<DeviceSession>> ListAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DeviceSession>>(
                sessions.Where(session => session.UserId == userId).ToArray());

        public Task<IReadOnlyList<DeviceSession>> ListActiveByDeviceAsync(
            Guid userId,
            string deviceId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<DeviceSession>>(sessions.Where(session =>
                session.UserId == userId &&
                session.DeviceId == deviceId &&
                session.RevokedAt is null).ToArray());
    }

    private sealed class FakeRevoker : ISessionTokenRevoker
    {
        public List<string> AuthorizationIds { get; } = [];

        public Task RevokeAsync(
            string authorizationId,
            CancellationToken cancellationToken = default)
        {
            AuthorizationIds.Add(authorizationId);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeUnitOfWork : IIdentityUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(1);
    }
}
