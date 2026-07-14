namespace Nevma.Contracts.Commands;

public sealed record PreviewCommandRequest(string Transcript, string IdempotencyKey);

public sealed record CommandResponse(
    Guid Id,
    CommandIntent Intent,
    CommandStatus Status,
    string Summary,
    string ArgumentsJson,
    bool RequiresConfirmation,
    string? ResultResource,
    string? Error,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ExecutedAt);

public enum CommandIntent { CreateTask, CreateMeeting }
public enum CommandStatus { Previewed, Executing, Executed, Failed, Undone }
