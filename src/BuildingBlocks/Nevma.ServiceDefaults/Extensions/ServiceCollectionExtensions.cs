using System.Diagnostics;
using Nevma.ServiceDefaults.Middleware;

namespace Nevma.ServiceDefaults.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddNevmaServiceDefaults(this IServiceCollection services)
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

        return services;
    }
}
