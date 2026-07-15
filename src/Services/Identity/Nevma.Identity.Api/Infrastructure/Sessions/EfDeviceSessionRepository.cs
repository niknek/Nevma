using Microsoft.EntityFrameworkCore;
using Nevma.Identity.Api.Application.Sessions;
using Nevma.Identity.Api.Domain.Sessions;
using Nevma.Identity.Api.Infrastructure.Persistence;

namespace Nevma.Identity.Api.Infrastructure.Sessions;

public sealed class EfDeviceSessionRepository(IdentityDbContext dbContext) : IDeviceSessionRepository
{
    public async Task AddAsync(DeviceSession session, CancellationToken cancellationToken = default) =>
        await dbContext.DeviceSessions.AddAsync(session, cancellationToken);

    public Task<DeviceSession?> GetAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.DeviceSessions.SingleOrDefaultAsync(
            session => session.Id == id && session.UserId == userId,
            cancellationToken);

    public async Task<IReadOnlyList<DeviceSession>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.DeviceSessions
            .AsNoTracking()
            .Where(session => session.UserId == userId)
            .OrderBy(session => session.RevokedAt != null)
            .ThenByDescending(session => session.LastSeenAt)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<DeviceSession>> ListActiveByDeviceAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default) =>
        await dbContext.DeviceSessions
            .Where(session =>
                session.UserId == userId &&
                session.DeviceId == deviceId &&
                session.RevokedAt == null)
            .ToListAsync(cancellationToken);
}
