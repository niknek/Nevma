using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Planning.Api.Domain.Tasks;

namespace Nevma.Planning.Api.Infrastructure.Persistence.Configurations;

public sealed class TaskShareConfiguration : IEntityTypeConfiguration<TaskShare>
{
    public void Configure(EntityTypeBuilder<TaskShare> builder)
    {
        builder.ToTable("task_shares", "planning");
        builder.HasKey(item => new { item.TaskId, item.UserId });
        builder.Property(item => item.SharedAt).IsRequired();
        builder.HasIndex(item => item.UserId);
        builder.HasOne<TaskItem>()
            .WithMany()
            .HasForeignKey(item => item.TaskId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
