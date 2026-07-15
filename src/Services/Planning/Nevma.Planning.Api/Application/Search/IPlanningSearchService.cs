using Nevma.Contracts.Search;

namespace Nevma.Planning.Api.Application.Search;

public interface IPlanningSearchService
{
    Task<IReadOnlyList<SearchResultItem>> SearchAsync(
        Guid userId,
        string query,
        int limit,
        CancellationToken cancellationToken = default);
}
