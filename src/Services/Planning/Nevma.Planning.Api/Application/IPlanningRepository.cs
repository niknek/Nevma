using Nevma.Planning.Api.Domain.Calendar;
using Nevma.Planning.Api.Domain.MeetingInvitations;

namespace Nevma.Planning.Api.Application;

public interface IPlanningRepository
{
    Task AddInvitationAsync(MeetingInvitation invitation, CancellationToken cancellationToken = default);
    Task<MeetingInvitation?> GetInvitationAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddCalendarEventAsync(CalendarEvent calendarEvent, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CalendarEvent>> GetCalendarAsync(
        Guid userId,
        DateTimeOffset from,
        DateTimeOffset to,
        CancellationToken cancellationToken = default);
}
