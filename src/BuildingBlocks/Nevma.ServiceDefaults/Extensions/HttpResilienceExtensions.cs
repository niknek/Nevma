using System.Diagnostics.Metrics;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.CircuitBreaker;
using Polly.Timeout;

namespace Nevma.ServiceDefaults.Extensions;

public static class HttpResilienceExtensions
{
    internal const string MeterName = "Nevma.Resilience";
    private static readonly Meter Meter = new(MeterName);
    private static readonly Counter<long> RetryCounter = Meter.CreateCounter<long>(
        "nevma.http.client.retries",
        description: "Transient HTTP dependency retries.");
    private static readonly Counter<long> TimeoutCounter = Meter.CreateCounter<long>(
        "nevma.http.client.timeouts",
        description: "HTTP dependency attempt timeouts.");
    private static readonly Counter<long> CircuitOpenedCounter = Meter.CreateCounter<long>(
        "nevma.http.client.circuit.opened",
        description: "HTTP dependency circuit breaker openings.");

    public static IHttpClientBuilder AddNevmaResilience(
        this IHttpClientBuilder builder,
        TimeSpan? totalRequestTimeout = null,
        TimeSpan? attemptTimeout = null)
    {
        var totalTimeout = totalRequestTimeout ?? TimeSpan.FromSeconds(15);
        var singleAttemptTimeout = attemptTimeout ?? TimeSpan.FromSeconds(5);
        var clientName = builder.Name;
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
            options.Retry.OnRetry = _ =>
            {
                RetryCounter.Add(1, new KeyValuePair<string, object?>("http.client.name", clientName));
                return default;
            };
            options.CircuitBreaker.FailureRatio = 0.5;
            options.CircuitBreaker.MinimumThroughput = 5;
            options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(
                Math.Max(30, singleAttemptTimeout.TotalSeconds * 2));
            options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
            options.CircuitBreaker.OnOpened = _ =>
            {
                CircuitOpenedCounter.Add(1, new KeyValuePair<string, object?>("http.client.name", clientName));
                return default;
            };
            options.AttemptTimeout.OnTimeout = _ =>
            {
                TimeoutCounter.Add(1, new KeyValuePair<string, object?>("http.client.name", clientName));
                return default;
            };
        });
        return builder;
    }

    public static bool IsTransientHttpFailure(
        this Exception exception,
        CancellationToken cancellationToken) =>
        exception is HttpRequestException or TimeoutRejectedException or BrokenCircuitException ||
        exception is TaskCanceledException && !cancellationToken.IsCancellationRequested;
}
