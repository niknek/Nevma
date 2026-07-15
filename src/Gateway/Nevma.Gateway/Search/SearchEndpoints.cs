using System.Net.Http.Headers;

namespace Nevma.Gateway.Search;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/search", async (
            string q,
            int? limit,
            HttpRequest request,
            GlobalSearchService service,
            CancellationToken cancellationToken) =>
        {
            var query = q.Trim();
            if (query.Length is < 2 or > 100)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["q"] = ["Search text must contain between 2 and 100 characters."]
                });
            if (!AuthenticationHeaderValue.TryParse(request.Headers.Authorization, out var authorization) ||
                !string.Equals(authorization.Scheme, "Bearer", StringComparison.OrdinalIgnoreCase))
                return Results.Unauthorized();

            return Results.Ok(await service.SearchAsync(
                query,
                limit ?? 20,
                authorization,
                cancellationToken));
        })
        .RequireAuthorization()
        .WithTags("Search");

        return endpoints;
    }
}
