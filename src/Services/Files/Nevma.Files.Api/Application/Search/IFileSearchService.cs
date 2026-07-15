using Nevma.Contracts.Search;

namespace Nevma.Files.Api.Application.Search;

public interface IFileSearchService
{
    Task<IReadOnlyList<SearchResultItem>> SearchAsync(
        Guid userId,
        string query,
        int limit,
        CancellationToken cancellationToken = default);
}
