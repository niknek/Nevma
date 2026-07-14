using Nevma.Notifications.Api.Domain.PushDevices;

namespace Nevma.Notifications.Api.Application.PushDevices;

public interface IPushDeviceRepository
{
    Task AddAsync(PushDevice device, CancellationToken cancellationToken = default);
    Task<PushDevice?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<PushDevice?> FindAsync(
        Guid userId,
        string deviceId,
        CancellationToken cancellationToken = default);
}
