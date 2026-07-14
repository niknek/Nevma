using System.Security.Claims;
using Nevma.Planning.Api.Application;

namespace Nevma.Planning.Api.Endpoints;

public static class CalendarEndpoints
{
    public static IEndpointRouteBuilder MapCalendarEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/calendar", async (
            DateTimeOffset? from,
            DateTimeOffset? to,
            ClaimsPrincipal principal,
            PlanningService service,
            TimeProvider timeProvider,
            CancellationToken cancellationToken) =>
        {
            if (!Guid.TryParse(principal.FindFirstValue("sub"), out var userId))
                return Results.Unauthorized();

            var now = timeProvider.GetUtcNow();
            var start = from ?? new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
            var end = to ?? start.AddDays(30);
            if (end <= start || end - start > TimeSpan.FromDays(366))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    [nameof(to)] = ["The calendar range must be positive and no longer than 366 days."]
                });
            }

            return Results.Ok(await service.GetCalendarAsync(userId, start, end, cancellationToken));
        })
        .RequireAuthorization()
        .WithTags("Calendar");

        return endpoints;
    }
}
