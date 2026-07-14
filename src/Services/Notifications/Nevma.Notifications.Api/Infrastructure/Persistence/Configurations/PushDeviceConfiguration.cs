using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Notifications.Api.Domain.PushDevices;

namespace Nevma.Notifications.Api.Infrastructure.Persistence.Configurations;

public sealed class PushDeviceConfiguration : IEntityTypeConfiguration<PushDevice>
{
    public void Configure(EntityTypeBuilder<PushDevice> builder)
    {
        builder.ToTable("push_devices", "notifications");
        builder.HasKey(device => device.Id);
        builder.Property(device => device.Id).ValueGeneratedNever();
        builder.Property(device => device.UserId).IsRequired();
        builder.Property(device => device.DeviceId).HasMaxLength(200).IsRequired();
        builder.Property(device => device.Platform).HasConversion<string>().HasMaxLength(20);
        builder.Property(device => device.ProtectedToken).HasMaxLength(8_192).IsRequired();
        builder.Property(device => device.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(device => device.IsActive).IsRequired();
        builder.Property(device => device.RegisteredAt).IsRequired();
        builder.Property(device => device.LastSeenAt).IsRequired();
        builder.Property<uint>("xmin").IsRowVersion();
        builder.HasIndex(device => new { device.UserId, device.DeviceId }).IsUnique();
        builder.HasIndex(device => new { device.IsActive, device.TokenHash });
    }
}
