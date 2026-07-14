using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Nevma.Commands.Api.Application;
using Nevma.Commands.Api.Domain;
using Nevma.Contracts.Commands;
using Nevma.Contracts.Planning;

namespace Nevma.Commands.Api.Infrastructure.Execution;

public sealed class PlanningCommandExecutor(IHttpClientFactory clientFactory) : ICommandExecutor
{
    public async Task<ExecutionResult> ExecuteAsync(
        CommandRequest command, string accessToken, CancellationToken cancellationToken = default)
    {
        var client = clientFactory.CreateClient("Planning");
        using var request = new HttpRequestMessage(HttpMethod.Post,
            command.Intent == CommandIntent.CreateTask ? "/api/tasks" : "/api/meeting-invitations");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Content = new StringContent(command.ArgumentsJson, System.Text.Encoding.UTF8, "application/json");
        using var response = await client.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
            return new ExecutionResult(false, null, $"Planning rejected the command with status {(int)response.StatusCode}.");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync(cancellationToken));
        if (!document.RootElement.TryGetProperty("id", out var id) || !id.TryGetGuid(out var resourceId))
            return new ExecutionResult(false, null, "Planning response did not contain a resource identifier.");
        var resource = command.Intent == CommandIntent.CreateTask
            ? $"tasks/{resourceId:N}" : $"meeting-invitations/{resourceId:N}";
        return new ExecutionResult(true, resource, null);
    }

    public async Task<bool> UndoAsync(
        CommandRequest command, string accessToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.ResultResource)) return false;
        var client = clientFactory.CreateClient("Planning");
        var parts = command.ResultResource.Split('/');
        if (parts.Length != 2 || !Guid.TryParse(parts[1], out var id)) return false;
        using var request = command.Intent == CommandIntent.CreateTask
            ? new HttpRequestMessage(HttpMethod.Delete, $"/api/tasks/{id}")
            : new HttpRequestMessage(HttpMethod.Post, $"/api/meeting-invitations/{id}/cancel");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        using var response = await client.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }
}
