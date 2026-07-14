using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Planning.Api.Infrastructure.Outbox;

namespace Nevma.Planning.Api.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages", "planning");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.Type).HasMaxLength(200).IsRequired();
        builder.Property(message => message.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(message => message.OccurredAt).IsRequired();
        builder.Property(message => message.Attempts).IsRequired();
        builder.Property(message => message.NextAttemptAt).IsRequired();
        builder.Property(message => message.ProcessedAt);
        builder.Property(message => message.LastError).HasMaxLength(2_000);
        builder.HasIndex(message => new { message.ProcessedAt, message.NextAttemptAt });
    }
}
