using Nevma.Contracts.Integration;
using Nevma.Contracts.Planning;
using Nevma.Notifications.Api.Application.Notifications;
using Nevma.Notifications.Api.Domain.Notifications;

namespace Nevma.Notifications.Api.Application.Integration;

public sealed class PlanningEventHandler(
    INotificationRepository notificationRepository,
    IIntegrationEventInbox inbox,
    INotificationsUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<PlanningEventHandleResult> HandleAsync(
        MeetingInvitationChangedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        if (await inbox.ExistsAsync(integrationEvent.EventId, cancellationToken))
            return PlanningEventHandleResult.AlreadyProcessed;
        if (integrationEvent.OrganizerId == integrationEvent.InviteeId)
            throw new InvalidDataException("Meeting participants must be different users.");

        var now = timeProvider.GetUtcNow();
        var title = GetPrivacySafeTitle(integrationEvent.Status);
        foreach (var userId in new[]
                 {
                     integrationEvent.OrganizerId,
                     integrationEvent.InviteeId
                 }.Distinct())
        {
            await notificationRepository.AddAsync(
                Notification.Create(
                    userId,
                    "meeting-invitation.changed",
                    title,
                    "Open Nevma to review this update.",
                    now),
                cancellationToken);
        }

        inbox.Add(
            integrationEvent.EventId,
            PlanningIntegrationEventTypes.MeetingInvitationChanged,
            now);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateNotificationEventException)
        {
            return PlanningEventHandleResult.AlreadyProcessed;
        }
        return PlanningEventHandleResult.Processed;
    }

    private static string GetPrivacySafeTitle(MeetingInvitationStatus status) =>
        status switch
        {
            MeetingInvitationStatus.Pending => "New meeting request",
            MeetingInvitationStatus.Accepted => "Meeting updated",
            MeetingInvitationStatus.Declined => "Meeting response",
            MeetingInvitationStatus.CounterProposed => "Meeting change proposed",
            MeetingInvitationStatus.RescheduleProposed => "Meeting change proposed",
            MeetingInvitationStatus.Cancelled => "Meeting cancelled",
            _ => "Meeting update"
        };
}

public enum PlanningEventHandleResult
{
    Processed,
    AlreadyProcessed
}
