using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Nevma.ServiceDefaults.Middleware;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Nevma.ServiceDefaults.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNevmaServiceDefaults(
        this IServiceCollection services,
        IConfiguration? configuration = null)
    {
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.TraceId.ToString() ?? context.HttpContext.TraceIdentifier;
                context.ProblemDetails.Extensions["correlationId"] = context.HttpContext.TraceIdentifier;
            };
        });
        services.AddExceptionHandler<NevmaExceptionHandler>();
        services.AddHealthChecks();
        AddObservability(services, configuration);

        return services;
    }

    private static void AddObservability(IServiceCollection services, IConfiguration? configuration)
    {
        var serviceName = System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "Nevma.Service";
        var otlpEnabled = configuration?.GetValue<bool>("OpenTelemetry:Otlp:Enabled") == true;
        var endpoint = configuration?["OpenTelemetry:Otlp:Endpoint"];

        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName))
            .WithTracing(tracing =>
            {
                tracing.AddAspNetCoreInstrumentation(options =>
                    options.RecordException = true);
                tracing.AddHttpClientInstrumentation(options =>
                    options.RecordException = true);
                if (otlpEnabled)
                    tracing.AddOtlpExporter(options => ConfigureEndpoint(options, endpoint));
            })
            .WithMetrics(metrics =>
            {
                metrics.AddAspNetCoreInstrumentation();
                metrics.AddHttpClientInstrumentation();
                metrics.AddRuntimeInstrumentation();
                if (otlpEnabled)
                    metrics.AddOtlpExporter(options => ConfigureEndpoint(options, endpoint));
            });

        services.AddLogging(logging => logging.AddOpenTelemetry(options =>
        {
            options.IncludeFormattedMessage = true;
            options.IncludeScopes = true;
            if (otlpEnabled)
                options.AddOtlpExporter(exporter => ConfigureEndpoint(exporter, endpoint));
        }));
    }

    private static void ConfigureEndpoint(OpenTelemetry.Exporter.OtlpExporterOptions options, string? endpoint)
    {
        if (!string.IsNullOrWhiteSpace(endpoint))
            options.Endpoint = new Uri(endpoint, UriKind.Absolute);
    }
}
