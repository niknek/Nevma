using Microsoft.EntityFrameworkCore;
using Nevma.Identity.Api.Application;
using Nevma.Identity.Api.Application.Users;
using Nevma.Identity.Api.Infrastructure.Persistence;
using Nevma.Identity.Api.Infrastructure.Users;

namespace Nevma.Identity.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("IdentityDatabase"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
                    npgsqlOptions.EnableRetryOnFailure();
                }));
        services.AddScoped<IIdentityUnitOfWork>(provider => provider.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<UserService>();
        return services;
    }
}
