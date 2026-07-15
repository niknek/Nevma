using Microsoft.EntityFrameworkCore;
using Nevma.Identity.Api.Application.Security;
using Nevma.Identity.Api.Domain.Security;
using Nevma.Identity.Api.Infrastructure.Persistence;

namespace Nevma.Identity.Api.Infrastructure.Security;

public sealed class EfSecurityEventRepository(IdentityDbContext dbContext) : ISecurityEventRepository
{
    public async Task AddAsync(SecurityEvent securityEvent, CancellationToken cancellationToken = default) =>
        await dbContext.SecurityEvents.AddAsync(securityEvent, cancellationToken);

    public async Task<IReadOnlyList<SecurityEvent>> ListAsync(
        Guid userId,
        int limit,
        CancellationToken cancellationToken = default) =>
        await dbContext.SecurityEvents
            .AsNoTracking()
            .Where(item => item.UserId == userId)
            .OrderByDescending(item => item.OccurredAt)
            .Take(limit)
            .ToArrayAsync(cancellationToken);
}
