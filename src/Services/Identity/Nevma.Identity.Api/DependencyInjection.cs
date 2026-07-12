using Nevma.Identity.Api.Application.Users;
using Nevma.Identity.Api.Infrastructure.Users;

namespace Nevma.Identity.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityService(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IUserRepository, InMemoryUserRepository>();
        services.AddSingleton<UserService>();
        return services;
    }
}
