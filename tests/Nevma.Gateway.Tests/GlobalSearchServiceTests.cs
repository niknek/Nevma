using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Nevma.Contracts.Search;
using Nevma.Gateway.Search;

namespace Nevma.Gateway.Tests;

public sealed class GlobalSearchServiceTests
{
    [Fact]
    public async Task Search_merges_available_sources_and_reports_partial_failures()
    {
        var now = DateTimeOffset.UtcNow;
        var factory = new TestHttpClientFactory(new Dictionary<string, HttpResponseMessage>
        {
            ["Planning"] = JsonResponse(new SearchResponse("roadmap", [
                new SearchResultItem("planning", "task", Guid.NewGuid(), null, "Roadmap", null, now.AddMinutes(-1))
            ], [])),
            ["Messaging"] = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable),
            ["Files"] = JsonResponse(new SearchResponse("roadmap", [
                new SearchResultItem("files", "file", Guid.NewGuid(), null, "roadmap.pdf", "application/pdf", now)
            ], []))
        });
        var service = new GlobalSearchService(
            factory,
            NullLogger<GlobalSearchService>.Instance);

        var result = await service.SearchAsync(
            "roadmap",
            20,
            new AuthenticationHeaderValue("Bearer", "test-token"));

        Assert.Equal(2, result.Items.Count);
        Assert.Equal("files", result.Items[0].Source);
        Assert.Equal(["messaging"], result.UnavailableSources);
        Assert.All(factory.Requests, request =>
            Assert.Equal("test-token", request.Headers.Authorization?.Parameter));
    }

    private static HttpResponseMessage JsonResponse(SearchResponse response) =>
        new(HttpStatusCode.OK) { Content = JsonContent.Create(response) };

    private sealed class TestHttpClientFactory(
        IReadOnlyDictionary<string, HttpResponseMessage> responses) : IHttpClientFactory
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        public HttpClient CreateClient(string name) =>
            new(new Handler(request =>
            {
                Requests.Add(request);
                return responses[name];
            }))
            {
                BaseAddress = new Uri($"https://{name.ToLowerInvariant()}.test")
            };
    }

    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(respond(request));
    }
}
