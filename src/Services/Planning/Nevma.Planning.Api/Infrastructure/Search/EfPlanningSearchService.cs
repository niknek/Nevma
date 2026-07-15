using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Search;
using Nevma.Planning.Api.Application.Search;
using Nevma.Planning.Api.Domain.Calendar;
using Nevma.Planning.Api.Infrastructure.Persistence;

namespace Nevma.Planning.Api.Infrastructure.Search;

public sealed class EfPlanningSearchService(PlanningDbContext dbContext) : IPlanningSearchService
{
    public async Task<IReadOnlyList<SearchResultItem>> SearchAsync(
        Guid userId,
        string query,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var normalized = query.Trim().ToLowerInvariant();
        var take = Math.Clamp(limit, 1, 50);
        var tasks = await dbContext.Tasks
            .AsNoTracking()
            .Where(item => (item.OwnerId == userId || dbContext.TaskShares.Any(share =>
                    share.TaskId == item.Id && share.UserId == userId)) &&
                item.DeletedAt == null &&
                (item.Title.ToLower().Contains(normalized) ||
                 (item.Notes != null && item.Notes.ToLower().Contains(normalized))))
            .OrderByDescending(item => item.UpdatedAt ?? item.CreatedAt)
            .Take(take)
            .Select(item => new { item.Id, item.Title, item.Notes, UpdatedAt = item.UpdatedAt ?? item.CreatedAt })
            .ToArrayAsync(cancellationToken);
        var events = await dbContext.CalendarEvents
            .AsNoTracking()
            .Where(item => item.ParticipantIds.Contains(userId) &&
                item.Status != CalendarEventStatus.Cancelled &&
                (item.Title.ToLower().Contains(normalized) ||
                 (item.Location != null && item.Location.ToLower().Contains(normalized))))
            .OrderByDescending(item => item.UpdatedAt)
            .Take(take)
            .Select(item => new { item.Id, item.Title, item.Location, item.UpdatedAt })
            .ToArrayAsync(cancellationToken);

        return tasks.Select(item => new SearchResultItem(
                "planning", "task", item.Id, null, item.Title, Truncate(item.Notes), item.UpdatedAt))
            .Concat(events.Select(item => new SearchResultItem(
                "planning", "calendar-event", item.Id, null, item.Title, Truncate(item.Location), item.UpdatedAt)))
            .OrderByDescending(item => item.UpdatedAt)
            .Take(take)
            .ToArray();
    }

    private static string? Truncate(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Length <= 240 ? value : value[..240];
}
