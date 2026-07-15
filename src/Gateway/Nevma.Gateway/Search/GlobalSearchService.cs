using System.Net.Http.Headers;
using System.Net.Http.Json;
using Nevma.Contracts.Search;

namespace Nevma.Gateway.Search;

public sealed class GlobalSearchService(
    IHttpClientFactory clientFactory,
    ILogger<GlobalSearchService> logger)
{
    private static readonly string[] Sources = ["Planning", "Messaging", "Files"];

    public async Task<SearchResponse> SearchAsync(
        string query,
        int limit,
        AuthenticationHeaderValue authorization,
        CancellationToken cancellationToken = default)
    {
        var take = Math.Clamp(limit, 1, 50);
        var tasks = Sources.Select(source => SearchSourceAsync(
            source,
            query,
            take,
            authorization,
            cancellationToken));
        var results = await Task.WhenAll(tasks);
        var items = results
            .Where(result => result.Response is not null)
            .SelectMany(result => result.Response!.Items)
            .OrderByDescending(item => item.UpdatedAt)
            .Take(take)
            .ToArray();
        var unavailable = results
            .Where(result => result.Response is null)
            .Select(result => result.Source.ToLowerInvariant())
            .ToArray();
        return new SearchResponse(query, items, unavailable);
    }

    private async Task<SourceResult> SearchSourceAsync(
        string source,
        string query,
        int limit,
        AuthenticationHeaderValue authorization,
        CancellationToken cancellationToken)
    {
        try
        {
            var client = clientFactory.CreateClient(source);
            using var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"/api/search?q={Uri.EscapeDataString(query)}&limit={limit}");
            request.Headers.Authorization = authorization;
            using var response = await client.SendAsync(
                request,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "Search source {Source} returned status {StatusCode}.",
                    source,
                    (int)response.StatusCode);
                return new SourceResult(source, null);
            }

            return new SourceResult(
                source,
                await response.Content.ReadFromJsonAsync<SearchResponse>(cancellationToken));
        }
        catch (Exception exception) when (IsSourceFailure(exception, cancellationToken))
        {
            logger.LogWarning(exception, "Search source {Source} is unavailable.", source);
            return new SourceResult(source, null);
        }
    }

    private static bool IsSourceFailure(Exception exception, CancellationToken cancellationToken) =>
        exception is HttpRequestException or System.Text.Json.JsonException ||
        exception is OperationCanceledException && !cancellationToken.IsCancellationRequested ||
        exception.GetType().Name is "TimeoutRejectedException" or "BrokenCircuitException";

    private sealed record SourceResult(string Source, SearchResponse? Response);
}
