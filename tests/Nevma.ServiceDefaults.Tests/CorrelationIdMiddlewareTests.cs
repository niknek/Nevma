using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Nevma.ServiceDefaults.Middleware;

namespace Nevma.ServiceDefaults.Tests;

public sealed class CorrelationIdMiddlewareTests
{
    [Fact]
    public async Task Valid_incoming_correlation_id_is_reused()
    {
        const string expected = "mobile-request_123";
        var context = CreateContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = expected;
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        Assert.Equal(expected, context.TraceIdentifier);
        Assert.Equal(expected, context.Response.Headers[CorrelationIdMiddleware.HeaderName]);
    }

    [Fact]
    public async Task Invalid_incoming_correlation_id_is_replaced()
    {
        var context = CreateContext();
        context.Request.Headers[CorrelationIdMiddleware.HeaderName] = "invalid value";
        var middleware = CreateMiddleware();

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        Assert.NotEqual("invalid value", context.TraceIdentifier);
        Assert.Matches("^[a-zA-Z0-9._-]{1,64}$", context.TraceIdentifier);
    }

    private static CorrelationIdMiddleware CreateMiddleware() =>
        new(_ => Task.CompletedTask, NullLogger<CorrelationIdMiddleware>.Instance);

    private static DefaultHttpContext CreateContext() => new()
    {
        Response =
        {
            Body = new MemoryStream()
        }
    };
}
