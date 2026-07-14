using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Messaging.Api.Domain.Conversations;
using Nevma.Messaging.Api.Domain.Messages;

namespace Nevma.Messaging.Api.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("messages", "messaging");
        builder.HasKey(message => message.Id);
        builder.Property(message => message.Id).ValueGeneratedNever();
        builder.Property(message => message.Sequence).UseIdentityAlwaysColumn();
        builder.Property(message => message.SenderId).IsRequired();
        builder.Property(message => message.Text).HasMaxLength(4_000).IsRequired();
        builder.Property(message => message.ReplyToMessageId);
        builder.Property(message => message.SentAt).IsRequired();
        builder.Property(message => message.EditedAt);
        builder.Property(message => message.DeletedAt);
        builder.Property<uint>("xmin").IsRowVersion();
        builder.HasIndex(message => message.Sequence).IsUnique();
        builder.HasIndex(message => new { message.ConversationId, message.Sequence });
        builder.HasOne<Conversation>()
            .WithMany()
            .HasForeignKey(message => message.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Message>()
            .WithMany()
            .HasForeignKey(message => message.ReplyToMessageId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(message => message.Attachments)
            .WithOne()
            .HasForeignKey(attachment => attachment.MessageId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(message => message.Attachments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class MessageAttachmentConfiguration : IEntityTypeConfiguration<MessageAttachment>
{
    public void Configure(EntityTypeBuilder<MessageAttachment> builder)
    {
        builder.ToTable("message_attachments", "messaging");
        builder.HasKey(attachment => new { attachment.MessageId, attachment.FileAssetId });
        builder.HasIndex(attachment => attachment.FileAssetId);
    }
}
