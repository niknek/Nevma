using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Identity.Api.Domain.Sessions;

namespace Nevma.Identity.Api.Infrastructure.Persistence.Configurations;

public sealed class DeviceSessionConfiguration : IEntityTypeConfiguration<DeviceSession>
{
    public void Configure(EntityTypeBuilder<DeviceSession> builder)
    {
        builder.ToTable("device_sessions", "identity");
        builder.HasKey(session => session.Id);
        builder.Property(session => session.DeviceId).HasMaxLength(128).IsRequired();
        builder.Property(session => session.DeviceName).HasMaxLength(100).IsRequired();
        builder.Property(session => session.Platform).HasMaxLength(30).IsRequired();
        builder.Property(session => session.AuthorizationId).HasMaxLength(100).IsRequired();
        builder.HasIndex(session => new { session.UserId, session.DeviceId });
        builder.HasIndex(session => session.AuthorizationId).IsUnique();
    }
}
