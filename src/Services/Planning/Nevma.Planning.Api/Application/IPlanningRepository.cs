using Nevma.Planning.Api.Domain.Calendar;
using Nevma.Planning.Api.Domain.MeetingInvitations;

namespace Nevma.Planning.Api.Application;

public interface IPlanningRepository
{
    void AddInvitation(MeetingInvitation invitation);
    MeetingInvitation? GetInvitation(Guid id);
    void AddCalendarEvent(CalendarEvent calendarEvent);
    IReadOnlyCollection<CalendarEvent> GetCalendar(Guid userId, DateTimeOffset from, DateTimeOffset to);
}
