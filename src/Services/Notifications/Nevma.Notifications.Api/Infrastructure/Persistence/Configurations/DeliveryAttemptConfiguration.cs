using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Notifications.Api.Domain.Delivery;
using Nevma.Notifications.Api.Domain.Notifications;
using Nevma.Notifications.Api.Domain.PushDevices;

namespace Nevma.Notifications.Api.Infrastructure.Persistence.Configurations;

public sealed class DeliveryAttemptConfiguration : IEntityTypeConfiguration<DeliveryAttempt>
{
    public void Configure(EntityTypeBuilder<DeliveryAttempt> builder)
    {
        builder.ToTable("delivery_attempts", "notifications");
        builder.HasKey(attempt => attempt.Id);
        builder.Property(attempt => attempt.Id).ValueGeneratedNever();
        builder.Property(attempt => attempt.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(attempt => attempt.CreatedAt).IsRequired();
        builder.Property(attempt => attempt.NextAttemptAt).IsRequired();
        builder.Property(attempt => attempt.ProviderMessageId).HasMaxLength(500);
        builder.Property(attempt => attempt.LastError).HasMaxLength(2_000);
        builder.Property(attempt => attempt.LockId);
        builder.Property(attempt => attempt.LockedUntil);
        builder.HasIndex(attempt => new
        {
            attempt.Status,
            attempt.NextAttemptAt,
            attempt.LockedUntil
        });
        builder.HasOne<Notification>()
            .WithMany()
            .HasForeignKey(attempt => attempt.NotificationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<PushDevice>()
            .WithMany()
            .HasForeignKey(attempt => attempt.PushDeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
