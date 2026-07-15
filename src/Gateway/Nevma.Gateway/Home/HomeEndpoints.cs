namespace Nevma.Gateway.Home;

public static class HomeEndpoints
{
    public static IEndpointRouteBuilder MapHomeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/home", async (
            HttpRequest request,
            HomeService service,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return Results.Ok(await service.GetAsync(
                    request.Headers.Authorization.ToString(),
                    cancellationToken));
            }
            catch (HomeAggregationException)
            {
                return Results.Problem(
                    statusCode: StatusCodes.Status503ServiceUnavailable,
                    title: "Home is temporarily unavailable.",
                    extensions: new Dictionary<string, object?>
                    {
                        ["code"] = "home.unavailable"
                    });
            }
        })
        .RequireAuthorization()
        .WithTags("Home");

        return endpoints;
    }
}
