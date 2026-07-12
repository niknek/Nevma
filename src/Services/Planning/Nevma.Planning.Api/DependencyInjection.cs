using Nevma.Planning.Api.Application;
using Nevma.Planning.Api.Infrastructure;

namespace Nevma.Planning.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPlanningService(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPlanningRepository, InMemoryPlanningRepository>();
        services.AddSingleton<PlanningService>();
        return services;
    }
}
