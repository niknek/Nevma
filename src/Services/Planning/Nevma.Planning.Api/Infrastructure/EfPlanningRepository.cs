using Microsoft.EntityFrameworkCore;
using Nevma.Planning.Api.Application;
using Nevma.Planning.Api.Domain.Calendar;
using Nevma.Planning.Api.Domain.MeetingInvitations;
using Nevma.Planning.Api.Infrastructure.Persistence;

namespace Nevma.Planning.Api.Infrastructure;

public sealed class EfPlanningRepository(PlanningDbContext dbContext) : IPlanningRepository
{
    public async Task AddInvitationAsync(
        MeetingInvitation invitation,
        CancellationToken cancellationToken = default) =>
        await dbContext.MeetingInvitations.AddAsync(invitation, cancellationToken);

    public Task<MeetingInvitation?> GetInvitationAsync(
        Guid id,
        CancellationToken cancellationToken = default) =>
        dbContext.MeetingInvitations.SingleOrDefaultAsync(invitation => invitation.Id == id, cancellationToken);

    public async Task AddCalendarEventAsync(
        CalendarEvent calendarEvent,
        CancellationToken cancellationToken = default) =>
        await dbContext.CalendarEvents.AddAsync(calendarEvent, cancellationToken);

    public Task<CalendarEvent?> GetCalendarEventAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default) =>
        dbContext.CalendarEvents.SingleOrDefaultAsync(
            calendarEvent => calendarEvent.InvitationId == invitationId,
            cancellationToken);

    public Task<bool> HasCalendarConflictAsync(
        Guid firstParticipantId,
        Guid secondParticipantId,
        DateTimeOffset startsAt,
        DateTimeOffset endsAt,
        Guid? excludedEventId,
        CancellationToken cancellationToken = default) =>
        dbContext.CalendarEvents.AnyAsync(
            calendarEvent =>
                calendarEvent.Status == CalendarEventStatus.Confirmed &&
                calendarEvent.Id != excludedEventId &&
                calendarEvent.StartsAt < endsAt &&
                calendarEvent.EndsAt > startsAt &&
                (calendarEvent.ParticipantIds.Contains(firstParticipantId) ||
                 calendarEvent.ParticipantIds.Contains(secondParticipantId)),
            cancellationToken);

    public async Task<IReadOnlyList<CalendarEvent>> GetCalendarAsync(
        Guid userId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await dbContext.CalendarEvents
            .AsNoTracking()
            .Where(calendarEvent =>
                calendarEvent.Status == CalendarEventStatus.Confirmed &&
                calendarEvent.ParticipantIds.Contains(userId) &&
                calendarEvent.StartsAt < to &&
                calendarEvent.EndsAt > from)
            .OrderBy(calendarEvent => calendarEvent.StartsAt)
            .ToListAsync(cancellationToken);
}
