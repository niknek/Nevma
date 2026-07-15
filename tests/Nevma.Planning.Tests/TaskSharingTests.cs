using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Planning;
using Nevma.Planning.Api.Application.Tasks;
using Nevma.Planning.Api.Infrastructure.Persistence;
using Nevma.Planning.Api.Infrastructure.Tasks;

namespace Nevma.Planning.Tests;

public sealed class TaskSharingTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Editor_can_list_and_complete_a_shared_task()
    {
        await using var context = CreateContext();
        var tasks = CreateTaskService(context);
        var sharing = CreateSharingService(context);
        var ownerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var created = await tasks.CreateAsync(ownerId, CreateRequest("Shared roadmap"));

        var share = await sharing.ShareAsync(
            created.Task!.Id,
            ownerId,
            new ShareTaskRequest(editorId, CanEdit: true));
        var shared = await sharing.ListSharedAsync(editorId);
        var completed = await tasks.CompleteAsync(created.Task.Id, editorId);

        Assert.IsType<TaskShareResult.Changed>(share);
        Assert.True(Assert.Single(shared).CanEdit);
        Assert.IsType<CompleteTaskResult.Completed>(completed);
    }

    [Fact]
    public async Task Viewer_cannot_edit_or_delegate_a_shared_task()
    {
        await using var context = CreateContext();
        var tasks = CreateTaskService(context);
        var sharing = CreateSharingService(context);
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var created = await tasks.CreateAsync(ownerId, CreateRequest("Read-only roadmap"));
        await sharing.ShareAsync(
            created.Task!.Id,
            ownerId,
            new ShareTaskRequest(viewerId, CanEdit: false));

        var update = await tasks.UpdateAsync(
            created.Task.Id,
            viewerId,
            new UpdateTaskRequest("Tampered", null, null, TaskPriority.Normal, null));
        var delegateAgain = await sharing.ShareAsync(
            created.Task.Id,
            viewerId,
            new ShareTaskRequest(Guid.NewGuid(), CanEdit: true));

        Assert.IsType<ChangeTaskResult.NotFound>(update);
        Assert.IsType<TaskShareResult.NotFound>(delegateAgain);
    }

    private static TaskService CreateTaskService(PlanningDbContext context) =>
        new(new EfTaskRepository(context), context, new FixedTimeProvider());

    private static TaskSharingService CreateSharingService(PlanningDbContext context) =>
        new(
            new EfTaskRepository(context),
            new EfTaskShareRepository(context),
            context,
            new FixedTimeProvider());

    private static PlanningDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase($"task-sharing-{Guid.NewGuid():N}")
            .Options);

    private static CreateTaskRequest CreateRequest(string title) =>
        new(title, null, null, TaskPriority.Normal, null);

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => Now;
    }
}
