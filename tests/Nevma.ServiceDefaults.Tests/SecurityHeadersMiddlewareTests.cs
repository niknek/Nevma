using Microsoft.AspNetCore.Http;
using Nevma.ServiceDefaults.Middleware;

namespace Nevma.ServiceDefaults.Tests;

public sealed class SecurityHeadersMiddlewareTests
{
    [Fact]
    public async Task Security_headers_are_added_to_the_response()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        var middleware = new SecurityHeadersMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);
        await context.Response.StartAsync();

        Assert.Equal("nosniff", context.Response.Headers.XContentTypeOptions);
        Assert.Equal("DENY", context.Response.Headers.XFrameOptions);
        Assert.Equal("no-referrer", context.Response.Headers["Referrer-Policy"]);
        Assert.Equal("camera=(), geolocation=(), microphone=()", context.Response.Headers["Permissions-Policy"]);
        Assert.Equal(
            "default-src 'none'; form-action 'self'; frame-ancestors 'none'",
            context.Response.Headers.ContentSecurityPolicy);
    }
}
