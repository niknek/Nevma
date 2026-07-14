using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Identity.Api.Domain.Connections;

namespace Nevma.Identity.Api.Infrastructure.Persistence.Configurations;

public sealed class ContactConnectionConfiguration : IEntityTypeConfiguration<ContactConnection>
{
    public void Configure(EntityTypeBuilder<ContactConnection> builder)
    {
        builder.ToTable("contact_connections", "identity");
        builder.HasKey(connection => connection.Id);
        builder.Property(connection => connection.Id).ValueGeneratedNever();
        builder.Property(connection => connection.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(connection => connection.CreatedAt).IsRequired();
        builder.HasIndex(connection => new { connection.PairFirstUserId, connection.PairSecondUserId }).IsUnique();
        builder.HasIndex(connection => new { connection.RequesterId, connection.Status });
        builder.HasIndex(connection => new { connection.AddresseeId, connection.Status });
        builder
            .HasOne<IdentityAccount>()
            .WithMany()
            .HasForeignKey(connection => connection.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);
        builder
            .HasOne<IdentityAccount>()
            .WithMany()
            .HasForeignKey(connection => connection.AddresseeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
