using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Nevma.ServiceDefaults.Extensions;

public static class HttpResilienceExtensions
{
    public static IHttpClientBuilder AddNevmaResilience(
        this IHttpClientBuilder builder,
        TimeSpan? totalRequestTimeout = null,
        TimeSpan? attemptTimeout = null)
    {
        var totalTimeout = totalRequestTimeout ?? TimeSpan.FromSeconds(15);
        var singleAttemptTimeout = attemptTimeout ?? TimeSpan.FromSeconds(5);
        if (totalTimeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(totalRequestTimeout));
        if (singleAttemptTimeout <= TimeSpan.Zero || singleAttemptTimeout >= totalTimeout)
            throw new ArgumentOutOfRangeException(nameof(attemptTimeout));

        builder.AddStandardResilienceHandler(options =>
        {
            options.TotalRequestTimeout.Timeout = totalTimeout;
            options.AttemptTimeout.Timeout = singleAttemptTimeout;
            options.Retry.MaxRetryAttempts = 2;
            options.Retry.Delay = TimeSpan.FromMilliseconds(200);
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.DisableForUnsafeHttpMethods();
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
        });
        return builder;
    }

    public static bool IsTransientHttpFailure(
        this Exception exception,
        CancellationToken cancellationToken) =>
        exception is HttpRequestException or TimeoutRejectedException or BrokenCircuitException ||
        exception is TaskCanceledException && !cancellationToken.IsCancellationRequested;
}
