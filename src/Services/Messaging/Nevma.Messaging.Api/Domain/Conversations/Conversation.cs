namespace Nevma.Messaging.Api.Domain.Conversations;

public sealed class Conversation
{
    private readonly List<ConversationParticipant> _participants = [];

    private Conversation(
        Guid id,
        ConversationKind kind,
        string? title,
        Guid createdBy,
        string? personalKey,
        DateTimeOffset createdAt)
    {
        Id = id;
        Kind = kind;
        Title = title;
        CreatedBy = createdBy;
        PersonalKey = personalKey;
        CreatedAt = createdAt;
    }

    public Guid Id { get; }
    public ConversationKind Kind { get; }
    public string? Title { get; private set; }
    public Guid CreatedBy { get; }
    public string? PersonalKey { get; }
    public DateTimeOffset CreatedAt { get; }
    public IReadOnlyCollection<ConversationParticipant> Participants => _participants.AsReadOnly();

    public static Conversation Create(
        ConversationKind kind,
        string? title,
        Guid createdBy,
        IReadOnlyCollection<Guid> participantIds,
        DateTimeOffset createdAt)
    {
        var ids = participantIds
            .Append(createdBy)
            .Distinct()
            .Order()
            .ToArray();
        var personalKey = kind == ConversationKind.Personal
            ? string.Join(':', ids.Select(id => id.ToString("N")))
            : null;
        var conversation = new Conversation(
            Guid.NewGuid(),
            kind,
            string.IsNullOrWhiteSpace(title) ? null : title.Trim(),
            createdBy,
            personalKey,
            createdAt);

        conversation._participants.AddRange(ids.Select(id =>
            ConversationParticipant.Create(conversation.Id, id, createdAt)));
        return conversation;
    }

    public bool Rename(string title)
    {
        if (Kind != ConversationKind.Group || string.IsNullOrWhiteSpace(title))
            return false;
        Title = title.Trim();
        return true;
    }

    public bool AddParticipant(Guid userId, DateTimeOffset joinedAt)
    {
        if (Kind != ConversationKind.Group || userId == Guid.Empty || _participants.Count >= 100 ||
            _participants.Any(participant => participant.UserId == userId))
            return false;
        _participants.Add(ConversationParticipant.Create(Id, userId, joinedAt));
        return true;
    }

    public bool RemoveParticipant(Guid userId)
    {
        if (Kind != ConversationKind.Group || userId == CreatedBy || _participants.Count <= 3)
            return false;
        var participant = _participants.SingleOrDefault(item => item.UserId == userId);
        return participant is not null && _participants.Remove(participant);
    }
}

public enum ConversationKind
{
    Personal,
    Group
}
