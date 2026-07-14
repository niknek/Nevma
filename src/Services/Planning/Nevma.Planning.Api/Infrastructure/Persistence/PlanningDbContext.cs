using Microsoft.EntityFrameworkCore;
using Nevma.Planning.Api.Application;
using Nevma.Planning.Api.Domain.Calendar;
using Nevma.Planning.Api.Domain.MeetingInvitations;
using Nevma.Planning.Api.Domain.Tasks;

namespace Nevma.Planning.Api.Infrastructure.Persistence;

public sealed class PlanningDbContext(DbContextOptions<PlanningDbContext> options)
    : DbContext(options), IPlanningUnitOfWork
{
    public DbSet<MeetingInvitation> MeetingInvitations => Set<MeetingInvitation>();
    public DbSet<CalendarEvent> CalendarEvents => Set<CalendarEvent>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("planning");
        builder.ApplyConfigurationsFromAssembly(typeof(PlanningDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new PlanningConcurrencyException(
                "Planning data was changed by another request.",
                exception);
        }
        catch (DbUpdateException exception) when (
            exception.Entries.Any(entry => entry.Entity is CalendarEvent))
        {
            throw new PlanningConcurrencyException(
                "A calendar event was already created for this invitation.",
                exception);
        }
    }
}
