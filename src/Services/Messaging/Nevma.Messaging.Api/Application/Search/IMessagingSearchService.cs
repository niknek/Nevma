using Nevma.Contracts.Search;

namespace Nevma.Messaging.Api.Application.Search;

public interface IMessagingSearchService
{
    Task<IReadOnlyList<SearchResultItem>> SearchAsync(
        Guid userId,
        string query,
        int limit,
        CancellationToken cancellationToken = default);
}
