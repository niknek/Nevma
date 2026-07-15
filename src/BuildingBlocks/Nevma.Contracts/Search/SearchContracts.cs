namespace Nevma.Contracts.Search;

public sealed record SearchResultItem(
    string Source,
    string Kind,
    Guid Id,
    Guid? ParentId,
    string Title,
    string? Snippet,
    DateTimeOffset UpdatedAt);

public sealed record SearchResponse(
    string Query,
    IReadOnlyList<SearchResultItem> Items,
    IReadOnlyList<string> UnavailableSources);
