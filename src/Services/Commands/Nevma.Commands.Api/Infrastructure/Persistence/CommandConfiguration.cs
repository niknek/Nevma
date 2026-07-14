using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Commands.Api.Domain;

namespace Nevma.Commands.Api.Infrastructure.Persistence;

public sealed class CommandConfiguration : IEntityTypeConfiguration<CommandRequest>
{
    public void Configure(EntityTypeBuilder<CommandRequest> builder)
    {
        builder.ToTable("command_requests", "commands");
        builder.HasKey(command => command.Id);
        builder.Property(command => command.Id).ValueGeneratedNever();
        builder.Property(command => command.IdempotencyKey).HasMaxLength(100).IsRequired();
        builder.Property(command => command.TranscriptHash).HasMaxLength(64).IsRequired();
        builder.Property(command => command.Intent).HasConversion<string>().HasMaxLength(30);
        builder.Property(command => command.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(command => command.Summary).HasMaxLength(500).IsRequired();
        builder.Property(command => command.ArgumentsJson).HasColumnType("jsonb").IsRequired();
        builder.Property(command => command.ResultResource).HasMaxLength(200);
        builder.Property(command => command.Error).HasMaxLength(500);
        builder.HasIndex(command => new { command.UserId, command.IdempotencyKey }).IsUnique();
        builder.HasIndex(command => new { command.UserId, command.CreatedAt });
    }
}
