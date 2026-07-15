using Microsoft.EntityFrameworkCore;
using Nevma.Commands.Api.Application;
using Nevma.Commands.Api.Infrastructure.Authentication;
using Nevma.Commands.Api.Infrastructure.Ai;
using Nevma.Commands.Api.Infrastructure.Execution;
using Nevma.Commands.Api.Infrastructure.Parsing;
using Nevma.Commands.Api.Infrastructure.Persistence;
using Nevma.Commands.Api.Infrastructure.Speech;

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
        services.Configure<AiProviderOptions>(configuration.GetSection(AiProviderOptions.SectionName));
        services.Configure<SpeechProviderOptions>(configuration.GetSection(SpeechProviderOptions.SectionName));
        services.AddSingleton<RuleBasedIntentParser>();
        services.AddSingleton<ICommandValidator, CommandValidator>();
        services.AddHttpClient<HttpAiProvider>(client => client.Timeout = TimeSpan.FromSeconds(20));
        services.AddHttpClient<HttpSpeechToTextProvider>(client => client.Timeout = TimeSpan.FromMinutes(2));
        services.AddTransient<IAiProvider, HttpAiProvider>();
        services.AddTransient<ISpeechToTextProvider, HttpSpeechToTextProvider>();
        services.AddTransient<IIntentParser, HybridIntentParser>();
        services.AddScoped<ICommandExecutor, BackendCommandExecutor>();
        services.AddScoped<CommandService>();
        services.AddHttpClient("Planning", client => client.BaseAddress = new Uri(configuration["Services:Planning"] ?? "http://localhost:5276"));
        services.AddHttpClient("Messaging", client => client.BaseAddress = new Uri(configuration["Services:Messaging"] ?? "http://localhost:5085"));
        return services;
    }
}
