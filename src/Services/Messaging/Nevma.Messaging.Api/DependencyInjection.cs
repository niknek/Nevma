using Microsoft.EntityFrameworkCore;
using Nevma.Messaging.Api.Application;
using Nevma.Messaging.Api.Application.Conversations;
using Nevma.Messaging.Api.Application.Messages;
using Nevma.Messaging.Api.Application.Integration;
using Nevma.Messaging.Api.Infrastructure.Authentication;
using Nevma.Messaging.Api.Infrastructure.Conversations;
using Nevma.Messaging.Api.Infrastructure.Messages;
using Nevma.Messaging.Api.Infrastructure.Persistence;
using Nevma.Messaging.Api.Infrastructure.Inbox;
using Nevma.Messaging.Api.Infrastructure.Messaging;
using Nevma.Messaging.Api.Infrastructure.Realtime;
using Nevma.Messaging.Api.Application.Presence;

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
        services.AddScoped<IIntegrationEventInbox, EfIntegrationEventInbox>();
        services.AddScoped<IUserRealtimePublisher, SignalRUserRealtimePublisher>();
        services.AddSingleton<IUserPresenceTracker, InMemoryUserPresenceTracker>();
        services.AddScoped<ConversationService>();
        services.AddScoped<MessageService>();
        services.AddScoped<PlanningEventHandler>();

        var brokerOptions = configuration
            .GetSection(MessageBrokerOptions.SectionName)
            .Get<MessageBrokerOptions>() ?? new MessageBrokerOptions();
        services.Configure<MessageBrokerOptions>(
            configuration.GetSection(MessageBrokerOptions.SectionName));
        if (brokerOptions.Enabled)
            services.AddHostedService<PlanningEventsConsumer>();
        return services;
    }
}
