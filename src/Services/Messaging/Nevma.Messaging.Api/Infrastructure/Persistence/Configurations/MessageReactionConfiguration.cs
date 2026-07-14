using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Messaging.Api.Domain.Messages;

namespace Nevma.Messaging.Api.Infrastructure.Persistence.Configurations;

public sealed class MessageReactionConfiguration : IEntityTypeConfiguration<MessageReaction>
{
    public void Configure(EntityTypeBuilder<MessageReaction> builder)
    {
        builder.ToTable("message_reactions", "messaging");
        builder.HasKey(reaction => new { reaction.MessageId, reaction.UserId });
        builder.Property(reaction => reaction.Emoji).HasMaxLength(32).IsRequired();
        builder.Property(reaction => reaction.CreatedAt).IsRequired();
        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(reaction => reaction.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
