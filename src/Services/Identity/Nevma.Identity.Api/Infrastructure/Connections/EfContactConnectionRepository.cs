using Microsoft.EntityFrameworkCore;
using Nevma.Identity.Api.Application.Connections;
using Nevma.Identity.Api.Domain.Connections;
using Nevma.Identity.Api.Infrastructure.Persistence;

namespace Nevma.Identity.Api.Infrastructure.Connections;

public sealed class EfContactConnectionRepository(IdentityDbContext dbContext)
    : IContactConnectionRepository
{
    public Task<ContactConnection?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.ContactConnections.SingleOrDefaultAsync(connection => connection.Id == id, cancellationToken);

    public Task<ContactConnection?> GetByUsersAsync(
        Guid firstUserId,
        Guid secondUserId,
        CancellationToken cancellationToken = default)
    {
        var pairFirst = Min(firstUserId, secondUserId);
        var pairSecond = Max(firstUserId, secondUserId);
        return dbContext.ContactConnections.SingleOrDefaultAsync(
            connection =>
                connection.PairFirstUserId == pairFirst && connection.PairSecondUserId == pairSecond,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ContactConnection>> ListForUserAsync(
        Guid userId,
        ContactConnectionStatus? status,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.ContactConnections
            .AsNoTracking()
            .Where(connection => connection.RequesterId == userId || connection.AddresseeId == userId);

        if (status is not null)
            query = query.Where(connection => connection.Status == status);

        return await query
            .OrderByDescending(connection => connection.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(ContactConnection connection, CancellationToken cancellationToken = default) =>
        await dbContext.ContactConnections.AddAsync(connection, cancellationToken);

    private static Guid Min(Guid first, Guid second) => first.CompareTo(second) <= 0 ? first : second;

    private static Guid Max(Guid first, Guid second) => first.CompareTo(second) >= 0 ? first : second;
}
