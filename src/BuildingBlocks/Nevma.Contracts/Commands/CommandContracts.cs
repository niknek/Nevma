namespace Nevma.Contracts.Commands;

public sealed record PreviewCommandRequest(string Transcript, string IdempotencyKey);

public sealed record SpeechTranscriptionResponse(string Transcript, double? Confidence);

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

public enum CommandIntent
{
    CreateTask,
    CompleteTask,
    DeleteTask,
    CreateMeeting,
    AcceptMeeting,
    DeclineMeeting,
    CancelMeeting,
    SendMessage
}
public enum CommandStatus { Previewed, Executing, Executed, Failed, Undone }
