using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Domain.Security;

namespace Nevma.Identity.Api.Application.Security;

public sealed class SecurityEventService(
    ISecurityEventRepository repository,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task RecordAsync(
        Guid userId,
        string eventType,
        bool succeeded,
        string? ipAddress,
        string? userAgent,
        string? correlationId,
        CancellationToken cancellationToken = default)
    {
        await repository.AddAsync(SecurityEvent.Create(
            userId,
            eventType,
            succeeded,
            ipAddress,
            userAgent,
            correlationId,
            timeProvider.GetUtcNow()), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SecurityEventResponse>> ListAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var events = await repository.ListAsync(userId, Math.Clamp(limit, 1, 100), cancellationToken);
        return events.Select(item => new SecurityEventResponse(
            item.Id,
            item.EventType,
            item.Succeeded,
            item.IpAddress,
            item.UserAgent,
            item.OccurredAt)).ToArray();
    }
}
