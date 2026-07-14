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
    public string? Title { get; }
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
}

public enum ConversationKind
{
    Personal,
    Group
}
