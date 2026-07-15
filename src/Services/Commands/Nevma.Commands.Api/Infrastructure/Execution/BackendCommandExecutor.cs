using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Nevma.Commands.Api.Application;
using Nevma.Commands.Api.Domain;
using Nevma.Contracts.Commands;

namespace Nevma.Commands.Api.Infrastructure.Execution;

public sealed class BackendCommandExecutor(IHttpClientFactory clientFactory) : ICommandExecutor
{
    public async Task<ExecutionResult> ExecuteAsync(
        CommandRequest command,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        var target = CreateTarget(command);
        if (target is null)
            return new ExecutionResult(false, null, "The command intent is not supported.");

        using var request = target.Request;
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (target.IncludeArguments)
            request.Content = new StringContent(command.ArgumentsJson, Encoding.UTF8, "application/json");

        var client = clientFactory.CreateClient(target.ClientName);
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new ExecutionResult(
                false,
                null,
                $"{target.ClientName} rejected the command with status {(int)response.StatusCode}.");

        if (command.Intent == CommandIntent.DeleteTask)
            return new ExecutionResult(true, target.ResourcePrefix, null);

        if (response.Content.Headers.ContentLength == 0)
            return new ExecutionResult(true, target.ResourcePrefix, null);

        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("id", out var id) || !id.TryGetGuid(out var resourceId))
            return new ExecutionResult(false, null, $"{target.ClientName} response did not contain a resource identifier.");

        var resource = command.Intent == CommandIntent.SendMessage
            ? $"{target.ResourcePrefix}/{resourceId:N}"
            : command.Intent is CommandIntent.CompleteTask or CommandIntent.AcceptMeeting or
                CommandIntent.DeclineMeeting or CommandIntent.CancelMeeting
                ? target.ResourcePrefix
                : $"{target.ResourcePrefix}/{resourceId:N}";
        return new ExecutionResult(true, resource, null);
    }

    public async Task<bool> UndoAsync(
        CommandRequest command,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ResultResource)) return false;

        var undo = command.Intent switch
        {
            CommandIntent.CreateTask => new UndoTarget("Planning", HttpMethod.Delete, $"/api/{command.ResultResource}"),
            CommandIntent.CreateMeeting => new UndoTarget("Planning", HttpMethod.Post, $"/api/{command.ResultResource}/cancel"),
            CommandIntent.SendMessage => new UndoTarget("Messaging", HttpMethod.Delete, $"/api/{command.ResultResource}"),
            _ => null
        };
        if (undo is null) return false;

        using var request = new HttpRequestMessage(undo.Method, undo.Path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await clientFactory.CreateClient(undo.ClientName).SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private static CommandTarget? CreateTarget(CommandRequest command)
    {
        ResourceCommandArguments? resource = null;
        SendMessageCommandArguments? message = null;
        if (command.Intent is CommandIntent.CompleteTask or CommandIntent.DeleteTask or
            CommandIntent.AcceptMeeting or CommandIntent.DeclineMeeting or CommandIntent.CancelMeeting)
            resource = JsonSerializer.Deserialize<ResourceCommandArguments>(command.ArgumentsJson);
        if (command.Intent == CommandIntent.SendMessage)
            message = JsonSerializer.Deserialize<SendMessageCommandArguments>(command.ArgumentsJson);

        return command.Intent switch
        {
            CommandIntent.CreateTask => new("Planning", new(HttpMethod.Post, "/api/tasks"), true, "tasks"),
            CommandIntent.CompleteTask when resource is not null => new(
                "Planning", new(HttpMethod.Post, $"/api/tasks/{resource.Id}/complete"), false, $"tasks/{resource.Id:N}"),
            CommandIntent.DeleteTask when resource is not null => new(
                "Planning", new(HttpMethod.Delete, $"/api/tasks/{resource.Id}"), false, $"tasks/{resource.Id:N}"),
            CommandIntent.CreateMeeting => new(
                "Planning", new(HttpMethod.Post, "/api/meeting-invitations"), true, "meeting-invitations"),
            CommandIntent.AcceptMeeting when resource is not null => MeetingAction(resource.Id, "accept"),
            CommandIntent.DeclineMeeting when resource is not null => MeetingAction(resource.Id, "decline"),
            CommandIntent.CancelMeeting when resource is not null => MeetingAction(resource.Id, "cancel"),
            CommandIntent.SendMessage when message is not null => new(
                "Messaging",
                new(HttpMethod.Post, $"/api/conversations/{message.ConversationId}/messages"),
                true,
                $"conversations/{message.ConversationId:N}/messages"),
            _ => null
        };
    }

    private static CommandTarget MeetingAction(Guid id, string action) => new(
        "Planning",
        new(HttpMethod.Post, $"/api/meeting-invitations/{id}/{action}"),
        false,
        $"meeting-invitations/{id:N}");

    private sealed record CommandTarget(
        string ClientName,
        HttpRequestMessage Request,
        bool IncludeArguments,
        string ResourcePrefix);
    private sealed record UndoTarget(string ClientName, HttpMethod Method, string Path);
}
