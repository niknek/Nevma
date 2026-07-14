using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Messaging.Api.Domain.Conversations;

namespace Nevma.Messaging.Api.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("conversations", "messaging");
        builder.HasKey(conversation => conversation.Id);
        builder.Property(conversation => conversation.Id).ValueGeneratedNever();
        builder.Property(conversation => conversation.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(conversation => conversation.Title).HasMaxLength(120);
        builder.Property(conversation => conversation.CreatedBy).IsRequired();
        builder.Property(conversation => conversation.PersonalKey).HasMaxLength(65);
        builder.Property(conversation => conversation.CreatedAt).IsRequired();
        builder.Property<uint>("xmin").IsRowVersion();
        builder.HasIndex(conversation => conversation.PersonalKey)
            .IsUnique()
            .HasFilter("\"PersonalKey\" IS NOT NULL");
        builder.HasMany(conversation => conversation.Participants)
            .WithOne()
            .HasForeignKey(participant => participant.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(conversation => conversation.Participants)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
