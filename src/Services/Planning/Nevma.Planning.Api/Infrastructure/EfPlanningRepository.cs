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

    public async Task<IReadOnlyList<CalendarEvent>> GetCalendarAsync(
        Guid userId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default) =>
        await dbContext.CalendarEvents
            .AsNoTracking()
            .Where(calendarEvent =>
                calendarEvent.ParticipantIds.Contains(userId) &&
                calendarEvent.StartsAt >= from &&
                calendarEvent.StartsAt < to)
            .OrderBy(calendarEvent => calendarEvent.StartsAt)
            .ToListAsync(cancellationToken);
}
