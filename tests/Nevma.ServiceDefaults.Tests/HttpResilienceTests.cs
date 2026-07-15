using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Nevma.ServiceDefaults.Extensions;

namespace Nevma.ServiceDefaults.Tests;

public sealed class HttpResilienceTests
{
    [Fact]
    public async Task Transient_get_failures_are_retried()
    {
        var handler = new RecordingHandler(
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.ServiceUnavailable,
            HttpStatusCode.OK);
        await using var provider = CreateProvider(handler);
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("dependency");

        using var response = await client.GetAsync("https://dependency.test/resource");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(3, handler.CallCount);
    }

    [Fact]
    public async Task Unsafe_requests_are_not_automatically_retried()
    {
        var handler = new RecordingHandler(HttpStatusCode.ServiceUnavailable, HttpStatusCode.OK);
        await using var provider = CreateProvider(handler);
        var client = provider.GetRequiredService<IHttpClientFactory>().CreateClient("dependency");

        using var response = await client.PostAsync(
            "https://dependency.test/resource",
            new StringContent("{}"));

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Equal(1, handler.CallCount);
    }

    private static ServiceProvider CreateProvider(HttpMessageHandler handler)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services
            .AddHttpClient("dependency")
            .ConfigurePrimaryHttpMessageHandler(() => handler)
            .AddNevmaResilience();
        return services.BuildServiceProvider();
    }

    private sealed class RecordingHandler(params HttpStatusCode[] statuses) : HttpMessageHandler
    {
        private int callCount;
        public int CallCount => callCount;

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var call = Interlocked.Increment(ref callCount);
            var status = statuses[Math.Min(call - 1, statuses.Length - 1)];
            return Task.FromResult(new HttpResponseMessage(status));
        }
    }
}
