using System.Text.Json;
using Nevma.Commands.Api.Application;
using Nevma.Commands.Api.Domain;
using Nevma.Contracts.Commands;
using Nevma.Contracts.Planning;

namespace Nevma.Commands.Api.Infrastructure.Parsing;

public sealed class RuleBasedIntentParser : IIntentParser
{
    public Task<ParseResult> ParseAsync(
        string transcript,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Parse(transcript, now));

    public ParseResult Parse(string transcript, DateTimeOffset now)
    {
        var text = transcript.Trim();
        if (StartsWith(text, "task:", "εργασία:")) return ParseTask(text);
        if (StartsWith(text, "complete task:", "ολοκλήρωσε εργασία:"))
            return ParseResource(text, CommandIntent.CompleteTask, "Complete task");
        if (StartsWith(text, "delete task:", "διέγραψε εργασία:"))
            return ParseResource(text, CommandIntent.DeleteTask, "Delete task");
        if (StartsWith(text, "meeting:", "συνάντηση:")) return ParseMeeting(text, now);
        if (StartsWith(text, "accept meeting:", "αποδέξου συνάντηση:"))
            return ParseResource(text, CommandIntent.AcceptMeeting, "Accept meeting");
        if (StartsWith(text, "decline meeting:", "απόρριψε συνάντηση:"))
            return ParseResource(text, CommandIntent.DeclineMeeting, "Decline meeting");
        if (StartsWith(text, "cancel meeting:", "ακύρωσε συνάντηση:"))
            return ParseResource(text, CommandIntent.CancelMeeting, "Cancel meeting");
        if (StartsWith(text, "message:", "μήνυμα:")) return ParseMessage(text);
        return new ParseResult.Invalid(
            "The command was not understood. Use a supported task, meeting, or message command.");
    }

    private static ParseResult ParseTask(string text)
    {
        var parts = Body(text).Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0 || parts[0].Length is 0 or > 200)
            return new ParseResult.Invalid("Task title is required.");
        DateTimeOffset? dueAt = null, reminderAt = null;
        var priority = TaskPriority.Normal;
        foreach (var part in parts.Skip(1))
        {
            var pair = SplitPair(part); if (pair is null) continue;
            if (pair.Value.Key == "due" && DateTimeOffset.TryParse(pair.Value.Value, out var due)) dueAt = due;
            if (pair.Value.Key == "reminder" && DateTimeOffset.TryParse(pair.Value.Value, out var reminder)) reminderAt = reminder;
            if (pair.Value.Key == "priority" && Enum.TryParse<TaskPriority>(pair.Value.Value, true, out var parsed)) priority = parsed;
        }
        var args = new CreateTaskRequest(parts[0], null, dueAt, priority, reminderAt);
        return new ParseResult.Parsed(new ParsedCommand(
            CommandIntent.CreateTask, $"Create task '{parts[0]}'", JsonSerializer.Serialize(args)));
    }

    private static ParseResult ParseMeeting(string text, DateTimeOffset now)
    {
        var parts = Body(text).Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2 || !Guid.TryParse(parts[0], out var inviteeId))
            return new ParseResult.Invalid("Meeting invitee must be a valid user identifier.");
        DateTimeOffset? startsAt = null; var duration = TimeSpan.FromHours(1); string? location = null;
        foreach (var part in parts.Skip(2))
        {
            var pair = SplitPair(part); if (pair is null) continue;
            if (pair.Value.Key == "starts" && DateTimeOffset.TryParse(pair.Value.Value, out var starts)) startsAt = starts;
            if (pair.Value.Key == "duration" && TimeSpan.TryParse(pair.Value.Value, out var parsed)) duration = parsed;
            if (pair.Value.Key == "location") location = pair.Value.Value;
        }
        if (startsAt is null || startsAt <= now)
            return new ParseResult.Invalid("Meeting start must be a valid future ISO date.");
        var args = new CreateMeetingInvitationRequest(inviteeId, parts[1], startsAt.Value, duration, location, null);
        return new ParseResult.Parsed(new ParsedCommand(
            CommandIntent.CreateMeeting, $"Invite a user to '{parts[1]}'", JsonSerializer.Serialize(args)));
    }

    private static ParseResult ParseResource(string text, CommandIntent intent, string summary)
    {
        if (!Guid.TryParse(Body(text), out var id))
            return new ParseResult.Invalid("The command must contain a valid resource identifier.");
        return new ParseResult.Parsed(new ParsedCommand(
            intent,
            $"{summary} {id}",
            JsonSerializer.Serialize(new ResourceCommandArguments(id))));
    }

    private static ParseResult ParseMessage(string text)
    {
        var parts = Body(text).Split('|', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2 || !Guid.TryParse(parts[0], out var conversationId))
            return new ParseResult.Invalid("Message conversation must be a valid identifier.");
        if (parts[1].Length is 0 or > 4_000)
            return new ParseResult.Invalid("Message text must contain 1 to 4000 characters.");
        return new ParseResult.Parsed(new ParsedCommand(
            CommandIntent.SendMessage,
            $"Send a message to conversation {conversationId}",
            JsonSerializer.Serialize(new SendMessageCommandArguments(conversationId, parts[1]))));
    }

    private static bool StartsWith(string text, params string[] prefixes) =>
        prefixes.Any(prefix => text.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    private static string Body(string text) => text[(text.IndexOf(':') + 1)..].Trim();
    private static KeyValuePair<string, string>? SplitPair(string part)
    {
        var index = part.IndexOf('=');
        return index <= 0 ? null : new(part[..index].Trim().ToLowerInvariant(), part[(index + 1)..].Trim());
    }
}
