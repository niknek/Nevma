using Nevma.Contracts.Identity;
using Nevma.Contracts.Notifications;
using Nevma.Contracts.Planning;

namespace Nevma.Contracts.Home;

public sealed record HomeResponse(
    TaskResponse? NextTask,
    IReadOnlyCollection<TaskResponse> UrgentTasks,
    IReadOnlyCollection<CalendarEventResponse> UpcomingEvents,
    IReadOnlyCollection<ContactConnectionResponse> PendingConnections,
    IReadOnlyCollection<NotificationResponse> UnreadNotifications,
    DateTimeOffset GeneratedAt);
