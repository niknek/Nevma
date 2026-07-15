using System.Net;
using System.Text.Json;
using Nevma.Commands.Api.Domain;
using Nevma.Commands.Api.Infrastructure.Execution;
using Nevma.Contracts.Commands;

namespace Nevma.Commands.Tests;

public sealed class BackendCommandExecutorTests
{
    [Fact]
    public async Task Task_sharing_is_confirmed_and_reversed_through_planning()
    {
        var taskId = Guid.NewGuid();
        var collaboratorId = Guid.NewGuid();
        var handler = new RecordingHandler();
        var executor = new BackendCommandExecutor(new TestHttpClientFactory(handler));
        var command = CommandRequest.Create(
            Guid.NewGuid(),
            "share-1",
            "share task",
            new ParsedCommand(
                CommandIntent.ShareTask,
                "Share task",
                JsonSerializer.Serialize(new ShareTaskCommandArguments(taskId, collaboratorId, true))),
            DateTimeOffset.UtcNow);

        var executed = await executor.ExecuteAsync(command, "access-token");
        Assert.True(executed.Success);
        Assert.Equal($"tasks/{taskId:N}/collaborators/{collaboratorId:N}", executed.Resource);
        command.BeginExecution(DateTimeOffset.UtcNow);
        command.Complete(executed.Resource!, DateTimeOffset.UtcNow);
        var undone = await executor.UndoAsync(command, "access-token");

        Assert.True(undone);
        Assert.Collection(
            handler.Requests,
            request =>
            {
                Assert.Equal(HttpMethod.Post, request.Method);
                Assert.Equal($"/api/tasks/{taskId}/collaborators", request.Path);
                Assert.Contains(collaboratorId.ToString(), request.Content!, StringComparison.OrdinalIgnoreCase);
            },
            request =>
            {
                Assert.Equal(HttpMethod.Delete, request.Method);
                Assert.Equal($"/api/tasks/{taskId:N}/collaborators/{collaboratorId:N}", request.Path);
            });
    }

    private sealed class TestHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri("https://planning.test") };
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(new RecordedRequest(
                request.Method,
                request.RequestUri!.AbsolutePath,
                request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken)));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{}", System.Text.Encoding.UTF8, "application/json")
            };
        }
    }

    private sealed record RecordedRequest(HttpMethod Method, string Path, string? Content);
}
