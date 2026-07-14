using Microsoft.EntityFrameworkCore;
using Nevma.Planning.Api.Application;
using Nevma.Planning.Api.Infrastructure;
using Nevma.Planning.Api.Infrastructure.Authentication;
using Nevma.Planning.Api.Infrastructure.Persistence;
using Nevma.Planning.Api.Application.Tasks;
using Nevma.Planning.Api.Infrastructure.Tasks;
using Nevma.Planning.Api.Infrastructure.Outbox;

namespace Nevma.Planning.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddPlanningService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<PlanningDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("PlanningDatabase"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "planning");
                    npgsqlOptions.EnableRetryOnFailure();
                }));
        services.AddPlanningAuthentication(configuration);
        services.AddScoped<IPlanningUnitOfWork>(provider => provider.GetRequiredService<PlanningDbContext>());
        services.AddScoped<IPlanningRepository, EfPlanningRepository>();
        services.AddScoped<IPlanningEventOutbox, EfPlanningEventOutbox>();
        services.AddScoped<PlanningService>();
        services.AddScoped<ITaskRepository, EfTaskRepository>();
        services.AddScoped<TaskService>();
        return services;
    }
}
