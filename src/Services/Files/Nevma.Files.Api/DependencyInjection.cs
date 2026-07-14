using Microsoft.EntityFrameworkCore;
using Nevma.Files.Api.Application;
using Nevma.Files.Api.Infrastructure.Authentication;
using Nevma.Files.Api.Infrastructure.Persistence;
using Nevma.Files.Api.Infrastructure.Scanning;
using Nevma.Files.Api.Infrastructure.Storage;

namespace Nevma.Files.Api;

public static class DependencyInjection
{
    public static IServiceCollection AddFilesService(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddDbContext<FilesDbContext>(options => options.UseNpgsql(
            configuration.GetConnectionString("FilesDatabase"),
            npgsql =>
            {
                npgsql.MigrationsHistoryTable("__EFMigrationsHistory", "files");
                npgsql.EnableRetryOnFailure();
            }));
        services.AddFilesAuthentication(configuration);
        services.AddScoped<IFilesUnitOfWork>(provider => provider.GetRequiredService<FilesDbContext>());
        services.AddScoped<IFileAssetRepository, EfFileAssetRepository>();
        services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddSingleton<IFileScanner, SafeContentScanner>();
        services.AddScoped<FileAssetService>();
        return services;
    }
}
