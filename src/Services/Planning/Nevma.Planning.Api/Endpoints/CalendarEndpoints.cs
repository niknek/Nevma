using Nevma.Planning.Api.Application;

namespace Nevma.Planning.Api.Endpoints;

public static class CalendarEndpoints
{
    public static IEndpointRouteBuilder MapCalendarEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/calendar", (
            Guid userId,
            DateTimeOffset? from,
            DateTimeOffset? to,
            PlanningService service,
            TimeProvider timeProvider) =>
        {
            var now = timeProvider.GetUtcNow();
            var start = from ?? new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
            var end = to ?? start.AddDays(30);
            return Results.Ok(service.GetCalendar(userId, start, end));
        }).WithTags("Calendar");

        return endpoints;
    }
}
