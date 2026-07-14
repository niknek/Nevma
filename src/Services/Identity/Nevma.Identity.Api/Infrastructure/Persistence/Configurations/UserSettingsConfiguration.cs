using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Identity.Api.Domain.Users;

namespace Nevma.Identity.Api.Infrastructure.Persistence.Configurations;

public sealed class UserSettingsConfiguration : IEntityTypeConfiguration<UserSettings>
{
    public void Configure(EntityTypeBuilder<UserSettings> builder)
    {
        builder.ToTable("user_settings", "identity");
        builder.HasKey(settings => settings.UserId);
        builder.Property(settings => settings.TimeZoneId).HasMaxLength(100).IsRequired();
        builder.Property(settings => settings.Locale).HasMaxLength(16).IsRequired();
        builder.Property(settings => settings.AllowPresence).IsRequired();
        builder.Property(settings => settings.UpdatedAt).IsRequired();
        builder.HasOne<User>().WithOne().HasForeignKey<UserSettings>(settings => settings.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
