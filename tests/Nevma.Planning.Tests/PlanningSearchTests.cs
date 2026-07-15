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
        context.Tasks.AddRange(
            TaskItem.Create(userId, "Prepare roadmap", "Q4 priorities", null, TaskPriority.High,
                null, TaskRecurrence.None, 1, null, DateTimeOffset.UtcNow),
            TaskItem.Create(Guid.NewGuid(), "Private roadmap", null, null, TaskPriority.Normal,
                null, TaskRecurrence.None, 1, null, DateTimeOffset.UtcNow));
        await context.SaveChangesAsync();
        var service = new EfPlanningSearchService(context);

        var results = await service.SearchAsync(userId, "roadmap", 20);

        var item = Assert.Single(results);
        Assert.Equal("Prepare roadmap", item.Title);
        Assert.Equal("task", item.Kind);
    }
}
