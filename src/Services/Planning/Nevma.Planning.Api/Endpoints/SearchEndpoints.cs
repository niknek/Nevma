using System.Security.Claims;
using Nevma.Contracts.Search;
using Nevma.Planning.Api.Application.Search;

namespace Nevma.Planning.Api.Endpoints;

public static class SearchEndpoints
{
    public static IEndpointRouteBuilder MapSearchEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/search", async (
            string q,
            int? limit,
            ClaimsPrincipal principal,
            IPlanningSearchService service,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(principal.FindFirstValue("sub"), out var userId))
                return Results.Unauthorized();
            var query = q.Trim();
            if (query.Length is < 2 or > 100)
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["q"] = ["Search text must contain between 2 and 100 characters."]
                });
            var items = await service.SearchAsync(userId, query, limit ?? 20, cancellationToken);
            return Results.Ok(new SearchResponse(query, items, []));
        })
        .RequireAuthorization()
        .WithTags("Search");

        return endpoints;
    }
}
