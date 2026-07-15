using System.Threading.RateLimiting;
using Nevma.Gateway.Home;

namespace Nevma.Gateway;

public static class DependencyInjection
{
    public static IServiceCollection AddGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services
            .AddReverseProxy()
            .LoadFromConfig(configuration.GetSection("ReverseProxy"));
        var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
        services.AddCors(options => options.AddPolicy("Gateway", policy =>
        {
            if (origins.Length > 0)
                policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
        }));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0,
                        AutoReplenishment = true
                    }));
        });
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<HomeService>();
        AddServiceClient(services, configuration, "Identity");
        AddServiceClient(services, configuration, "Planning");
        AddServiceClient(services, configuration, "Notifications");

        return services;
    }

    private static void AddServiceClient(
        IServiceCollection services,
        IConfiguration configuration,
        string name)
    {
        var address = configuration[$"Services:{name}"]
            ?? throw new InvalidOperationException($"Services:{name} is required.");
        services.AddHttpClient(name, client => client.BaseAddress = new Uri(address, UriKind.Absolute));
    }
}
