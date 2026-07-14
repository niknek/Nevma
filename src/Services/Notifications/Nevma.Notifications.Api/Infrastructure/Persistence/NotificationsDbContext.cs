using Microsoft.EntityFrameworkCore;
using Nevma.Notifications.Api.Application;
using Nevma.Notifications.Api.Domain.Notifications;
using Nevma.Notifications.Api.Domain.PushDevices;
using Nevma.Notifications.Api.Infrastructure.Inbox;
using Nevma.Notifications.Api.Application.Integration;
using Npgsql;

namespace Nevma.Notifications.Api.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options), INotificationsUnitOfWork
{
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PushDevice> PushDevices => Set<PushDevice>();
    public DbSet<InboxMessage> InboxMessages => Set<InboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("notifications");
        builder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await base.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "PK_inbox_messages"
            })
        {
            throw new DuplicateNotificationEventException(exception);
        }
    }
}
