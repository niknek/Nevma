using Microsoft.EntityFrameworkCore;
using Nevma.Commands.Api.Application;
using Nevma.Commands.Api.Infrastructure.Authentication;
using Nevma.Commands.Api.Infrastructure.Execution;
using Nevma.Commands.Api.Infrastructure.Parsing;
using Nevma.Commands.Api.Infrastructure.Persistence;

namespace Nevma.Commands.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddCommandsService(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<CommandsDbContext>(options => options.UseNpgsql(
            configuration.GetConnectionString("CommandsDatabase"),
            npgsql => { npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "commands"); npgsql.EnableRetryOnFailure(); }));
        services.AddCommandsAuthentication(configuration);
        services.AddScoped<ICommandsUnitOfWork>(provider => provider.GetRequiredService<CommandsDbContext>());
        services.AddScoped<ICommandRepository, EfCommandRepository>();
        services.AddSingleton<IIntentParser, RuleBasedIntentParser>();
        services.AddScoped<ICommandExecutor, PlanningCommandExecutor>();
        services.AddScoped<CommandService>();
        services.AddHttpClient("Planning", client => client.BaseAddress = new Uri(configuration["Services:Planning"] ?? "http://localhost:5276"));
        return services;
    }
}
