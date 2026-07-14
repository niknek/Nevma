using Nevma.Contracts.Messaging;
using Nevma.Messaging.Api.Domain.Conversations;
using ContractKind = Nevma.Contracts.Messaging.ConversationKind;
using DomainKind = Nevma.Messaging.Api.Domain.Conversations.ConversationKind;

namespace Nevma.Messaging.Api.Application.Conversations;

public sealed class ConversationService(
    IConversationRepository repository,
    IMessagingUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<CreateConversationResult> CreateAsync(
        Guid creatorId,
        CreateConversationRequest request,
        CancellationToken cancellationToken = default)
    {
        var participantIds = request.ParticipantIds ?? [];
        var errors = Validate(request, creatorId, participantIds);
        if (errors.Count > 0)
            return CreateConversationResult.Failure(errors);

        var kind = (DomainKind)(int)request.Kind;
        var conversation = Conversation.Create(
            kind,
            request.Title,
            creatorId,
            participantIds,
            timeProvider.GetUtcNow());

        if (conversation.PersonalKey is not null)
        {
            var existing = await repository.FindPersonalAsync(conversation.PersonalKey, cancellationToken);
            if (existing is not null)
                return CreateConversationResult.Success(ToResponse(existing), false);
        }

        await repository.AddAsync(conversation, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (DuplicatePersonalConversationException) when (conversation.PersonalKey is not null)
        {
            repository.Discard(conversation);
            var existing = await repository.FindPersonalAsync(conversation.PersonalKey, cancellationToken);
            if (existing is not null)
                return CreateConversationResult.Success(ToResponse(existing), false);
            throw;
        }
        return CreateConversationResult.Success(ToResponse(conversation), true);
    }

    public async Task<IReadOnlyList<ConversationResponse>> ListAsync(
        Guid userId,
        CancellationToken cancellationToken = default) =>
        (await repository.ListAsync(userId, cancellationToken))
            .Select(ToResponse)
            .ToArray();

    public Task<bool> CanAccessAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken = default) =>
        repository.IsParticipantAsync(conversationId, userId, cancellationToken);

    public async Task<ConversationChangeResult> RenameAsync(
        Guid conversationId,
        Guid actorId,
        string title,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length > 120)
            return new ConversationChangeResult.Invalid("Title must contain 1 to 120 characters.");
        var conversation = await repository.GetAsync(conversationId, cancellationToken);
        if (conversation is null)
            return new ConversationChangeResult.NotFound();
        if (conversation.CreatedBy != actorId)
            return new ConversationChangeResult.Forbidden();
        if (!conversation.Rename(title))
            return new ConversationChangeResult.Invalid("Only group conversations can be renamed.");
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ConversationChangeResult.Changed(ToResponse(conversation));
    }

    public async Task<ConversationChangeResult> AddParticipantAsync(
        Guid conversationId,
        Guid actorId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await repository.GetAsync(conversationId, cancellationToken);
        if (conversation is null)
            return new ConversationChangeResult.NotFound();
        if (conversation.CreatedBy != actorId)
            return new ConversationChangeResult.Forbidden();
        if (!conversation.AddParticipant(userId, timeProvider.GetUtcNow()))
            return new ConversationChangeResult.Invalid("Participant cannot be added to this conversation.");
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ConversationChangeResult.Changed(ToResponse(conversation));
    }

    public async Task<ConversationChangeResult> RemoveParticipantAsync(
        Guid conversationId,
        Guid actorId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var conversation = await repository.GetAsync(conversationId, cancellationToken);
        if (conversation is null)
            return new ConversationChangeResult.NotFound();
        if (conversation.CreatedBy != actorId && actorId != userId)
            return new ConversationChangeResult.Forbidden();
        if (!conversation.RemoveParticipant(userId))
            return new ConversationChangeResult.Invalid("Participant cannot be removed from this conversation.");
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ConversationChangeResult.Changed(ToResponse(conversation));
    }

    private static Dictionary<string, string[]> Validate(
        CreateConversationRequest request,
        Guid creatorId,
        IReadOnlyCollection<Guid> participantIds)
    {
        var errors = new Dictionary<string, string[]>();
        if (!Enum.IsDefined(request.Kind))
        {
            errors[nameof(request.Kind)] = ["Conversation kind is invalid."];
            return errors;
        }
        if (participantIds.Any(id => id == Guid.Empty))
            errors[nameof(request.ParticipantIds)] = ["Participant identifiers cannot be empty."];

        var participantCount = participantIds.Append(creatorId).Distinct().Count();
        if (request.Kind == ContractKind.Personal && participantCount != 2)
            errors[nameof(request.ParticipantIds)] = ["A personal conversation must have exactly two participants."];
        if (request.Kind == ContractKind.Group && participantCount is < 3 or > 100)
            errors[nameof(request.ParticipantIds)] = ["A group conversation must have between 3 and 100 participants."];
        if (request.Kind == ContractKind.Group && string.IsNullOrWhiteSpace(request.Title))
            errors[nameof(request.Title)] = ["A group conversation title is required."];
        else if (request.Title?.Length > 120)
            errors[nameof(request.Title)] = ["Conversation title cannot exceed 120 characters."];
        return errors;
    }

    private static ConversationResponse ToResponse(Conversation conversation) =>
        new(
            conversation.Id,
            (ContractKind)(int)conversation.Kind,
            conversation.Title,
            conversation.Participants.Select(participant => participant.UserId).ToArray(),
            conversation.CreatedAt,
            conversation.CreatedBy);
}

public abstract record ConversationChangeResult
{
    public sealed record Changed(ConversationResponse Conversation) : ConversationChangeResult;
    public sealed record NotFound : ConversationChangeResult;
    public sealed record Forbidden : ConversationChangeResult;
    public sealed record Invalid(string Message) : ConversationChangeResult;
}

public sealed record CreateConversationResult(
    ConversationResponse? Conversation,
    bool IsNew,
    IReadOnlyDictionary<string, string[]> Errors)
{
    public bool IsSuccess => Conversation is not null;

    public static CreateConversationResult Success(ConversationResponse conversation, bool isNew) =>
        new(conversation, isNew, new Dictionary<string, string[]>());

    public static CreateConversationResult Failure(IReadOnlyDictionary<string, string[]> errors) =>
        new(null, false, errors);
}
