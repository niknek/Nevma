using Microsoft.EntityFrameworkCore;
using Nevma.Files.Api.Application;
using Nevma.Files.Api.Domain;

namespace Nevma.Files.Api.Infrastructure.Persistence;

public sealed class EfFileAssetRepository(FilesDbContext dbContext) : IFileAssetRepository
{
    public async Task AddAsync(FileAsset asset, CancellationToken cancellationToken = default) =>
        await dbContext.FileAssets.AddAsync(asset, cancellationToken);

    public Task<FileAsset?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.FileAssets.Include(asset => asset.AccessGrants)
            .SingleOrDefaultAsync(asset => asset.Id == id, cancellationToken);

    public async Task<IReadOnlyList<FileAsset>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        await dbContext.FileAssets.AsNoTracking().Include(asset => asset.AccessGrants)
            .Where(asset => asset.DeletedAt == null &&
                (asset.OwnerId == userId || asset.AccessGrants.Any(grant => grant.UserId == userId)))
            .OrderByDescending(asset => asset.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);
}
