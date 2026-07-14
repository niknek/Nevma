using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Messaging.Api.Domain.Conversations;

namespace Nevma.Messaging.Api.Infrastructure.Persistence.Configurations;

public sealed class ConversationParticipantConfiguration
    : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("conversation_participants", "messaging");
        builder.HasKey(participant => new { participant.ConversationId, participant.UserId });
        builder.Property(participant => participant.JoinedAt).IsRequired();
        builder.HasIndex(participant => new { participant.UserId, participant.JoinedAt });
    }
}
