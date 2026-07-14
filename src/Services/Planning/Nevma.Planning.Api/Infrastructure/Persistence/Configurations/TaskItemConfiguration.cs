using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Planning.Api.Domain.Tasks;

namespace Nevma.Planning.Api.Infrastructure.Persistence.Configurations;

public sealed class TaskItemConfiguration : IEntityTypeConfiguration<TaskItem>
{
    public void Configure(EntityTypeBuilder<TaskItem> builder)
    {
        builder.ToTable("tasks", "planning");
        builder.HasKey(task => task.Id);
        builder.Property(task => task.Id).ValueGeneratedNever();
        builder.Property(task => task.OwnerId).IsRequired();
        builder.Property(task => task.Title).HasMaxLength(200).IsRequired();
        builder.Property(task => task.Notes).HasMaxLength(4_000);
        builder.Property(task => task.DueAt);
        builder.Property(task => task.Priority).HasConversion<string>().HasMaxLength(20);
        builder.Property(task => task.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(task => task.ReminderAt);
        builder.Property(task => task.CreatedAt).IsRequired();
        builder.Property(task => task.CompletedAt);
        builder.Property<uint>("xmin").IsRowVersion();
        builder.HasIndex(task => new { task.OwnerId, task.Status, task.DueAt });
        builder.HasIndex(task => new { task.OwnerId, task.Priority, task.Status });
        builder.HasIndex(task => new { task.Status, task.ReminderAt });
    }
}
