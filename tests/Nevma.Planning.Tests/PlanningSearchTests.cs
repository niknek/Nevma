using Microsoft.EntityFrameworkCore;
using Nevma.Planning.Api.Domain.Tasks;
using Nevma.Planning.Api.Infrastructure.Persistence;
using Nevma.Planning.Api.Infrastructure.Search;

namespace Nevma.Planning.Tests;

public sealed class PlanningSearchTests
{
    [Fact]
    public async Task Search_returns_only_the_callers_matching_tasks()
    {
        await using var context = new PlanningDbContext(
            new DbContextOptionsBuilder<PlanningDbContext>()
                .UseInMemoryDatabase($"planning-search-{Guid.NewGuid():N}")
                .Options);
        var userId = Guid.NewGuid();
        var owned = TaskItem.Create(userId, "Prepare roadmap", "Q4 priorities", null, TaskPriority.High,
            null, TaskRecurrence.None, 1, null, DateTimeOffset.UtcNow);
        var shared = TaskItem.Create(Guid.NewGuid(), "Shared roadmap", null, null, TaskPriority.Normal,
            null, TaskRecurrence.None, 1, null, DateTimeOffset.UtcNow);
        context.Tasks.AddRange(
            owned,
            shared,
            TaskItem.Create(Guid.NewGuid(), "Private roadmap", null, null, TaskPriority.Normal,
                null, TaskRecurrence.None, 1, null, DateTimeOffset.UtcNow));
        context.TaskShares.Add(TaskShare.Create(shared.Id, userId, true, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();
        var service = new EfPlanningSearchService(context);

        var results = await service.SearchAsync(userId, "roadmap", 20);

        Assert.Equal(2, results.Count);
        Assert.Contains(results, item => item.Title == "Prepare roadmap" && item.Kind == "task");
        Assert.Contains(results, item => item.Title == "Shared roadmap" && item.Kind == "task");
    }
}
