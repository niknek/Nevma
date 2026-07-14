using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Planning;
using Nevma.Planning.Api.Application.Tasks;
using Nevma.Planning.Api.Infrastructure.Persistence;
using Nevma.Planning.Api.Infrastructure.Tasks;

namespace Nevma.Planning.Tests;

public sealed class TaskServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DayStart = new(2026, 7, 14, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Authenticated_user_becomes_the_task_owner()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var ownerId = Guid.NewGuid();

        var result = await service.CreateAsync(ownerId, CreateRequest("Call Maria"));

        Assert.True(result.IsSuccess);
        var stored = await context.Tasks.SingleAsync();
        Assert.Equal(ownerId, stored.OwnerId);
        Assert.Equal("Call Maria", stored.Title);
    }

    [Fact]
    public async Task Another_user_cannot_complete_the_task()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var created = await service.CreateAsync(Guid.NewGuid(), CreateRequest("Private task"));

        var result = await service.CompleteAsync(created.Task!.Id, Guid.NewGuid());

        Assert.IsType<CompleteTaskResult.NotFound>(result);
        Assert.Null((await context.Tasks.SingleAsync()).CompletedAt);
    }

    [Fact]
    public async Task Completing_a_task_twice_is_rejected()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var ownerId = Guid.NewGuid();
        var created = await service.CreateAsync(ownerId, CreateRequest("Finish report"));

        var completed = await service.CompleteAsync(created.Task!.Id, ownerId);
        var repeated = await service.CompleteAsync(created.Task.Id, ownerId);

        Assert.IsType<CompleteTaskResult.Completed>(completed);
        Assert.IsType<CompleteTaskResult.AlreadyCompleted>(repeated);
    }

    [Fact]
    public async Task Filters_return_only_the_requested_tasks_for_the_owner()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var ownerId = Guid.NewGuid();
        var urgent = await service.CreateAsync(
            ownerId,
            CreateRequest("Urgent", priority: TaskPriority.Urgent));
        await service.CreateAsync(
            ownerId,
            CreateRequest("Today", dueAt: Now.AddHours(3)));
        await service.CreateAsync(
            ownerId,
            CreateRequest("Upcoming", dueAt: Now.AddDays(2)));
        var completed = await service.CreateAsync(ownerId, CreateRequest("Completed"));
        await service.CompleteAsync(completed.Task!.Id, ownerId);
        await service.CreateAsync(Guid.NewGuid(), CreateRequest("Someone else's urgent", priority: TaskPriority.Urgent));

        var urgentTasks = await service.ListAsync(ownerId, TaskFilter.Urgent, DayStart);
        var todayTasks = await service.ListAsync(ownerId, TaskFilter.Today, DayStart);
        var upcomingTasks = await service.ListAsync(ownerId, TaskFilter.Upcoming, DayStart);
        var completedTasks = await service.ListAsync(ownerId, TaskFilter.Completed, DayStart);

        Assert.Collection(urgentTasks, task => Assert.Equal(urgent.Task!.Id, task.Id));
        Assert.Collection(todayTasks, task => Assert.Equal("Today", task.Title));
        Assert.Collection(upcomingTasks, task => Assert.Equal("Upcoming", task.Title));
        Assert.Collection(completedTasks, task => Assert.Equal(completed.Task.Id, task.Id));
    }

    [Fact]
    public async Task Reminder_after_the_due_time_is_rejected()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var request = CreateRequest(
            "Impossible reminder",
            dueAt: Now.AddHours(1),
            reminderAt: Now.AddHours(2));

        var result = await service.CreateAsync(Guid.NewGuid(), request);

        Assert.False(result.IsSuccess);
        Assert.Contains(nameof(CreateTaskRequest.ReminderAt), result.Errors.Keys);
        Assert.Empty(context.Tasks);
    }

    [Fact]
    public async Task Owner_can_update_and_delete_an_active_task()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var ownerId = Guid.NewGuid();
        var created = await service.CreateAsync(ownerId, CreateRequest("Old title"));

        var updated = await service.UpdateAsync(
            created.Task!.Id,
            ownerId,
            new UpdateTaskRequest(
                "New title",
                "Changed",
                Now.AddDays(1),
                TaskPriority.High,
                Now.AddHours(1)));
        var deleted = await service.DeleteAsync(created.Task.Id, ownerId);
        var visible = await service.ListAsync(ownerId, TaskFilter.All, DayStart);

        Assert.Equal("New title", Assert.IsType<ChangeTaskResult.Updated>(updated).Task.Title);
        Assert.Equal(DeleteTaskResult.Deleted, deleted);
        Assert.Empty(visible);
    }

    [Fact]
    public async Task Completing_a_recurring_task_creates_the_next_occurrence()
    {
        await using var context = CreateContext();
        var service = CreateService(context);
        var ownerId = Guid.NewGuid();
        var dueAt = Now.AddDays(1);
        var created = await service.CreateAsync(
            ownerId,
            new CreateTaskRequest(
                "Daily check",
                null,
                dueAt,
                TaskPriority.Normal,
                Now.AddHours(12),
                TaskRecurrence.Daily));

        var result = Assert.IsType<CompleteTaskResult.Completed>(
            await service.CompleteAsync(created.Task!.Id, ownerId));

        Assert.NotNull(result.NextOccurrence);
        Assert.Equal(dueAt.AddDays(1), result.NextOccurrence.DueAt);
        Assert.Equal(2, await context.Tasks.CountAsync());
    }

    private static TaskService CreateService(PlanningDbContext context) =>
        new(new EfTaskRepository(context), context, new FixedTimeProvider(Now));

    private static PlanningDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<PlanningDbContext>()
            .UseInMemoryDatabase($"tasks-{Guid.NewGuid():N}")
            .Options;

        return new PlanningDbContext(options);
    }

    private static CreateTaskRequest CreateRequest(
        string title,
        DateTimeOffset? dueAt = null,
        TaskPriority priority = TaskPriority.Normal,
        DateTimeOffset? reminderAt = null) =>
        new(title, null, dueAt, priority, reminderAt);

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
