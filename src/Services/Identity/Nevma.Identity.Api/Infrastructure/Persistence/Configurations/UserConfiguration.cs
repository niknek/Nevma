using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Identity.Api.Domain.Users;

namespace Nevma.Identity.Api.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users", "identity");
        builder.HasKey(user => user.Id);
        builder.Property(user => user.Id).ValueGeneratedNever();
        builder.Property(user => user.DisplayName).HasMaxLength(100).IsRequired();
        builder.Property(user => user.AvatarUrl).HasMaxLength(2_048);
        builder.Property(user => user.CreatedAt).IsRequired();
    }
}
