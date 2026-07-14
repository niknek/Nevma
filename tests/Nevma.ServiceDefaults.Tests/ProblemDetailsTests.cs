using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Nevma.ServiceDefaults.Extensions;

namespace Nevma.ServiceDefaults.Tests;

public sealed class ProblemDetailsTests
{
    [Fact]
    public async Task Unhandled_exception_returns_safe_problem_details()
    {
        const string sensitiveMessage = "Database password was exposed.";
        await using var provider = new ServiceCollection()
            .AddLogging()
            .AddNevmaServiceDefaults()
            .BuildServiceProvider();
        var handler = Assert.Single(provider.GetServices<IExceptionHandler>());
        var context = new DefaultHttpContext
        {
            RequestServices = provider,
            TraceIdentifier = "request-123"
        };
        context.Request.Path = "/test";
        context.Response.Body = new MemoryStream();

        var handled = await handler.TryHandleAsync(
            context,
            new InvalidOperationException(sensitiveMessage),
            CancellationToken.None);

        context.Response.Body.Position = 0;
        using var response = await JsonDocument.ParseAsync(
            context.Response.Body,
            cancellationToken: CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.Equal("An unexpected error occurred.", response.RootElement.GetProperty("title").GetString());
        Assert.Equal("request-123", response.RootElement.GetProperty("correlationId").GetString());
        Assert.DoesNotContain(sensitiveMessage, response.RootElement.GetRawText(), StringComparison.Ordinal);
    }
}
