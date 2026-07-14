using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Planning.Api.Domain.MeetingInvitations;

namespace Nevma.Planning.Api.Infrastructure.Persistence.Configurations;

public sealed class MeetingInvitationConfiguration : IEntityTypeConfiguration<MeetingInvitation>
{
    public void Configure(EntityTypeBuilder<MeetingInvitation> builder)
    {
        builder.ToTable("meeting_invitations", "planning");
        builder.HasKey(invitation => invitation.Id);
        builder.Property(invitation => invitation.Id).ValueGeneratedNever();
        builder.Property(invitation => invitation.OrganizerId).IsRequired();
        builder.Property(invitation => invitation.InviteeId).IsRequired();
        builder.Property(invitation => invitation.Title).HasMaxLength(200).IsRequired();
        builder.Property(invitation => invitation.StartsAt).IsRequired();
        builder.Property(invitation => invitation.Duration).IsRequired();
        builder.Property(invitation => invitation.Location).HasMaxLength(500);
        builder.Property(invitation => invitation.Message).HasMaxLength(2_000);
        builder.Property(invitation => invitation.Status).HasConversion<string>().HasMaxLength(30);
        builder.Property(invitation => invitation.CreatedAt).IsRequired();
        builder.Property(invitation => invitation.RespondedAt);
        builder.Property(invitation => invitation.ProposedStartsAt);
        builder.Property(invitation => invitation.ProposedDuration);
        builder.Property(invitation => invitation.ProposedLocation).HasMaxLength(500);
        builder.Property<uint>("xmin").IsRowVersion();
        builder.HasIndex(invitation => new { invitation.InviteeId, invitation.Status, invitation.StartsAt });
        builder.HasIndex(invitation => new { invitation.OrganizerId, invitation.Status, invitation.StartsAt });
    }
}
