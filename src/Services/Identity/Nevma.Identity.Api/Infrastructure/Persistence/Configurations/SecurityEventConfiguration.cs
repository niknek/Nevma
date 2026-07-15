using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Identity.Api.Domain.Security;

namespace Nevma.Identity.Api.Infrastructure.Persistence.Configurations;

public sealed class SecurityEventConfiguration : IEntityTypeConfiguration<SecurityEvent>
{
    public void Configure(EntityTypeBuilder<SecurityEvent> builder)
    {
        builder.ToTable("security_events");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.EventType).HasMaxLength(80).IsRequired();
        builder.Property(item => item.IpAddress).HasMaxLength(45);
        builder.Property(item => item.UserAgent).HasMaxLength(256);
        builder.Property(item => item.CorrelationId).HasMaxLength(64);
        builder.HasIndex(item => new { item.UserId, item.OccurredAt });
    }
}
