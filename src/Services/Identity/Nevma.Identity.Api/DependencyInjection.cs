using Microsoft.EntityFrameworkCore;
using Nevma.Identity.Api.Application.Authentication;
using Nevma.Identity.Api.Application;
using Nevma.Identity.Api.Application.Connections;
using Nevma.Identity.Api.Application.Users;
using Nevma.Identity.Api.Infrastructure.Authentication;
using Nevma.Identity.Api.Infrastructure.Connections;
using Nevma.Identity.Api.Infrastructure.Persistence;
using Nevma.Identity.Api.Infrastructure.Users;

namespace Nevma.Identity.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddIdentityService(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
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
        services.AddNevmaAuthentication(configuration, environment);
        services.AddScoped<IIdentityUnitOfWork>(provider => provider.GetRequiredService<IdentityDbContext>());
        services.AddScoped<IContactConnectionRepository, EfContactConnectionRepository>();
        services.AddScoped<ContactConnectionService>();
        services.AddScoped<IUserRepository, EfUserRepository>();
        services.AddScoped<IUserPrivacyRepository, EfUserPrivacyRepository>();
        services.AddScoped<UserService>();
        services.AddScoped<UserPrivacyService>();
        services.AddScoped<IRegistrationService, RegistrationService>();
        services.AddHostedService<OpenIddictSeeder>();
        return services;
    }
}
