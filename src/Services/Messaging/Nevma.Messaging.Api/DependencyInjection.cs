using Microsoft.EntityFrameworkCore;
using Nevma.Messaging.Api.Application;
using Nevma.Messaging.Api.Application.Conversations;
using Nevma.Messaging.Api.Application.Messages;
using Nevma.Messaging.Api.Infrastructure.Authentication;
using Nevma.Messaging.Api.Infrastructure.Conversations;
using Nevma.Messaging.Api.Infrastructure.Messages;
using Nevma.Messaging.Api.Infrastructure.Persistence;

namespace Nevma.Messaging.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddMessagingService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<MessagingDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("MessagingDatabase"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "messaging");
                    npgsqlOptions.EnableRetryOnFailure();
                }));
        services.AddMessagingAuthentication(configuration);
        services.AddScoped<IMessagingUnitOfWork>(provider =>
            provider.GetRequiredService<MessagingDbContext>());
        services.AddScoped<IConversationRepository, EfConversationRepository>();
        services.AddScoped<IMessageRepository, EfMessageRepository>();
        services.AddScoped<ConversationService>();
        services.AddScoped<MessageService>();
        return services;
    }
}
