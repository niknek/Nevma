using Microsoft.EntityFrameworkCore;
using Nevma.Notifications.Api.Application;
using Nevma.Notifications.Api.Domain.Notifications;
using Nevma.Notifications.Api.Domain.PushDevices;

namespace Nevma.Notifications.Api.Infrastructure.Persistence;

public sealed class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options), INotificationsUnitOfWork
{
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<PushDevice> PushDevices => Set<PushDevice>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema("notifications");
        builder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
    }
}
