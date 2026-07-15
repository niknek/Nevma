using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Search;
using Nevma.Files.Api.Application.Search;
using Nevma.Files.Api.Infrastructure.Persistence;

namespace Nevma.Files.Api.Infrastructure.Search;

public sealed class EfFileSearchService(FilesDbContext dbContext) : IFileSearchService
{
    public async Task<IReadOnlyList<SearchResultItem>> SearchAsync(
        Guid userId,
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim().ToLowerInvariant();
        var items = await dbContext.FileAssets
            .AsNoTracking()
            .Where(asset => asset.DeletedAt == null && asset.FileName.ToLower().Contains(normalized) &&
                (asset.OwnerId == userId || asset.AccessGrants.Any(grant => grant.UserId == userId)))
            .OrderByDescending(asset => asset.CreatedAt)
            .Take(Math.Clamp(limit, 1, 50))
            .Select(asset => new SearchResultItem(
                "files",
                "file",
                asset.Id,
                null,
                asset.FileName,
                asset.ContentType,
                asset.CreatedAt))
            .ToArrayAsync(cancellationToken);
        return items;
    }
}
