using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Identity.Api.Domain.Users;

namespace Nevma.Identity.Api.Infrastructure.Persistence.Configurations;

public sealed class UserReportConfiguration : IEntityTypeConfiguration<UserReport>
{
    public void Configure(EntityTypeBuilder<UserReport> builder)
    {
        builder.ToTable("user_reports", "identity");
        builder.HasKey(report => report.Id);
        builder.Property(report => report.Id).ValueGeneratedNever();
        builder.Property(report => report.Reason).HasMaxLength(100).IsRequired();
        builder.Property(report => report.Details).HasMaxLength(2_000);
        builder.Property(report => report.CreatedAt).IsRequired();
        builder.HasIndex(report => new { report.ReportedUserId, report.CreatedAt });
        builder.HasOne<User>().WithMany().HasForeignKey(report => report.ReporterId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(report => report.ReportedUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
