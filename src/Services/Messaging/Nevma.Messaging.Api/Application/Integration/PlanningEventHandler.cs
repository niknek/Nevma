using Nevma.Contracts.Integration;
using Nevma.Contracts.Messaging;
using Nevma.Messaging.Api.Application.Conversations;

namespace Nevma.Messaging.Api.Application.Integration;

public sealed class PlanningEventHandler(
    ConversationService conversationService,
    IIntegrationEventInbox inbox,
    IUserRealtimePublisher realtimePublisher,
    IMessagingUnitOfWork unitOfWork,
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

        var conversation = await conversationService.CreateAsync(
            integrationEvent.OrganizerId,
            new CreateConversationRequest(
                ConversationKind.Personal,
                null,
                [integrationEvent.InviteeId]),
            cancellationToken);
        if (!conversation.IsSuccess)
            throw new InvalidDataException("A conversation could not be created for the meeting participants.");

        await realtimePublisher.PublishMeetingInvitationChangedAsync(
            [integrationEvent.OrganizerId, integrationEvent.InviteeId],
            integrationEvent,
            cancellationToken);
        inbox.Add(
            integrationEvent.EventId,
            PlanningIntegrationEventTypes.MeetingInvitationChanged,
            timeProvider.GetUtcNow());
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicateInboxEventException)
        {
            return PlanningEventHandleResult.AlreadyProcessed;
        }

        return PlanningEventHandleResult.Processed;
    }
}

public enum PlanningEventHandleResult
{
    Processed,
    AlreadyProcessed
}
