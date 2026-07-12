using System.Collections.Concurrent;
using Nevma.Planning.Api.Application;
using Nevma.Planning.Api.Domain.Calendar;
using Nevma.Planning.Api.Domain.MeetingInvitations;

namespace Nevma.Planning.Api.Infrastructure;

public sealed class InMemoryPlanningRepository : IPlanningRepository
{
    private readonly ConcurrentDictionary<Guid, MeetingInvitation> _invitations = new();
    private readonly ConcurrentDictionary<Guid, CalendarEvent> _events = new();

    public void AddInvitation(MeetingInvitation invitation) => _invitations[invitation.Id] = invitation;

    public MeetingInvitation? GetInvitation(Guid id) => _invitations.GetValueOrDefault(id);

    public void AddCalendarEvent(CalendarEvent calendarEvent) => _events[calendarEvent.Id] = calendarEvent;

    public IReadOnlyCollection<CalendarEvent> GetCalendar(
        Guid userId,
        DateTimeOffset from,
        DateTimeOffset to) =>
        _events.Values
            .Where(item => item.ParticipantIds.Contains(userId) && item.StartsAt >= from && item.StartsAt < to)
            .OrderBy(item => item.StartsAt)
            .ToArray();
}
