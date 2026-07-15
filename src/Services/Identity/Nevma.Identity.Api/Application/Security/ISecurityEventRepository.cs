using Nevma.Identity.Api.Domain.Security;

namespace Nevma.Identity.Api.Application.Security;

public interface ISecurityEventRepository
{
    Task AddAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SecurityEvent>> ListAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default);
}
