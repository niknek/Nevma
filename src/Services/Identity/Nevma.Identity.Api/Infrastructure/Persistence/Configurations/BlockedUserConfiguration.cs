using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Identity.Api.Domain.Users;

namespace Nevma.Identity.Api.Infrastructure.Persistence.Configurations;

public sealed class BlockedUserConfiguration : IEntityTypeConfiguration<BlockedUser>
{
    public void Configure(EntityTypeBuilder<BlockedUser> builder)
    {
        builder.ToTable("blocked_users", "identity");
        builder.HasKey(block => new { block.BlockerId, block.BlockedId });
        builder.Property(block => block.CreatedAt).IsRequired();
        builder.HasIndex(block => block.BlockedId);
        builder.HasOne<User>().WithMany().HasForeignKey(block => block.BlockerId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<User>().WithMany().HasForeignKey(block => block.BlockedId).OnDelete(DeleteBehavior.Cascade);
    }
}
