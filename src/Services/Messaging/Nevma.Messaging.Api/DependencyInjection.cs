using Nevma.Messaging.Api.Application.Messages;
using Nevma.Messaging.Api.Infrastructure.Messages;

namespace Nevma.Messaging.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddMessagingService(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IMessageRepository, InMemoryMessageRepository>();
        services.AddSingleton<MessageService>();
        return services;
    }
}
