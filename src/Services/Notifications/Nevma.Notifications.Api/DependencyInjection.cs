using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Nevma.Notifications.Api.Application;
using Nevma.Notifications.Api.Application.Notifications;
using Nevma.Notifications.Api.Application.PushDevices;
using Nevma.Notifications.Api.Infrastructure.Authentication;
using Nevma.Notifications.Api.Infrastructure.Notifications;
using Nevma.Notifications.Api.Infrastructure.Persistence;
using Nevma.Notifications.Api.Infrastructure.PushDevices;

namespace Nevma.Notifications.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationsService(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddDataProtection().SetApplicationName("Nevma.Notifications");
        services.AddDbContext<NotificationsDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("NotificationsDatabase"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "notifications");
                    npgsqlOptions.EnableRetryOnFailure();
                }));
        services.AddNotificationsAuthentication(configuration);
        services.AddScoped<INotificationsUnitOfWork>(provider =>
            provider.GetRequiredService<NotificationsDbContext>());
        services.AddScoped<INotificationRepository, EfNotificationRepository>();
        services.AddScoped<IPushDeviceRepository, EfPushDeviceRepository>();
        services.AddSingleton<IPushTokenProtector, DataProtectionPushTokenProtector>();
        services.AddScoped<NotificationService>();
        services.AddScoped<PushDeviceService>();
        return services;
    }
}
