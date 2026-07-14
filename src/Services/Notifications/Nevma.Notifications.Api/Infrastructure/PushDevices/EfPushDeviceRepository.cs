using Microsoft.EntityFrameworkCore;
using Nevma.Notifications.Api.Application.PushDevices;
using Nevma.Notifications.Api.Domain.PushDevices;
using Nevma.Notifications.Api.Infrastructure.Persistence;

namespace Nevma.Notifications.Api.Infrastructure.PushDevices;

public sealed class EfPushDeviceRepository(NotificationsDbContext dbContext) : IPushDeviceRepository
{
    public async Task AddAsync(PushDevice device, CancellationToken cancellationToken = default) =>
        await dbContext.PushDevices.AddAsync(device, cancellationToken);

    public Task<PushDevice?> GetAsync(
        Guid id,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        dbContext.PushDevices.SingleOrDefaultAsync(
            device => device.Id == id && device.UserId == userId,
            cancellationToken);

    public Task<PushDevice?> FindAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default) =>
        dbContext.PushDevices.SingleOrDefaultAsync(
            device => device.UserId == userId && device.DeviceId == deviceId,
            cancellationToken);
}
