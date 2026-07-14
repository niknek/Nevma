using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nevma.Planning.Api.Domain.Calendar;
using Nevma.Planning.Api.Domain.MeetingInvitations;

namespace Nevma.Planning.Api.Infrastructure.Persistence.Configurations;

public sealed class CalendarEventConfiguration : IEntityTypeConfiguration<CalendarEvent>
{
    public void Configure(EntityTypeBuilder<CalendarEvent> builder)
    {
        builder.ToTable("calendar_events", "planning");
        builder.HasKey(calendarEvent => calendarEvent.Id);
        builder.Property(calendarEvent => calendarEvent.Id).ValueGeneratedNever();
        builder.Property(calendarEvent => calendarEvent.InvitationId).IsRequired();
        builder.Property(calendarEvent => calendarEvent.Title).HasMaxLength(200).IsRequired();
        builder.Property(calendarEvent => calendarEvent.StartsAt).IsRequired();
        builder.Property(calendarEvent => calendarEvent.EndsAt).IsRequired();
        builder.Property(calendarEvent => calendarEvent.Location).HasMaxLength(500);
        builder.Property(calendarEvent => calendarEvent.ParticipantIds).HasColumnType("uuid[]").IsRequired();
        builder.HasIndex(calendarEvent => calendarEvent.InvitationId).IsUnique();
        builder.HasIndex(calendarEvent => calendarEvent.StartsAt);
        builder.HasIndex(calendarEvent => calendarEvent.ParticipantIds).HasMethod("gin");
        builder
            .HasOne<MeetingInvitation>()
            .WithOne()
            .HasForeignKey<CalendarEvent>(calendarEvent => calendarEvent.InvitationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
