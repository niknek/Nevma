using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Microsoft.EntityFrameworkCore;
using Nevma.Files.Api.Application;
using Nevma.Files.Api.Infrastructure.Authentication;
using Nevma.Files.Api.Infrastructure.Persistence;
using Nevma.Files.Api.Infrastructure.Processing;
using Nevma.Files.Api.Infrastructure.Scanning;
using Nevma.Files.Api.Infrastructure.Storage;
using Nevma.Files.Api.Application.Search;
using Nevma.Files.Api.Infrastructure.Search;

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
        services.AddScoped<IFileSearchService, EfFileSearchService>();
        services.Configure<S3StorageOptions>(configuration.GetSection(S3StorageOptions.SectionName));
        services.Configure<FileDeliveryOptions>(configuration.GetSection(S3StorageOptions.SectionName));
        services.Configure<ClamAvOptions>(configuration.GetSection(ClamAvOptions.SectionName));

        if (string.Equals(configuration["FileStorage:Provider"], "S3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAmazonS3>(_ => CreateS3Client(configuration));
            services.AddSingleton<IFileStorage, S3FileStorage>();
        }
        else
        {
            services.AddSingleton<IFileStorage, LocalFileStorage>();
        }

        services.AddSingleton<IFileScanner>(provider =>
            string.Equals(configuration["MalwareScanning:Provider"], "ClamAV", StringComparison.OrdinalIgnoreCase)
                ? ActivatorUtilities.CreateInstance<ClamAvScanner>(provider)
                : ActivatorUtilities.CreateInstance<SafeContentScanner>(provider));
        services.AddSingleton<IFileContentProcessor, SkiaFileContentProcessor>();
        services.AddScoped<FileAssetService>();
        return services;
    }

    private static IAmazonS3 CreateS3Client(IConfiguration configuration)
    {
        var region = configuration["FileStorage:Region"] ?? "eu-central-1";
        var serviceUrl = configuration["FileStorage:ServiceUrl"];
        var config = new AmazonS3Config
        {
            ForcePathStyle = configuration.GetValue("FileStorage:ForcePathStyle", true),
            AuthenticationRegion = region,
            RegionEndpoint = RegionEndpoint.GetBySystemName(region)
        };
        if (!string.IsNullOrWhiteSpace(serviceUrl))
            config.ServiceURL = serviceUrl;

        var accessKey = configuration["FileStorage:AccessKey"];
        var secretKey = configuration["FileStorage:SecretKey"];
        return !string.IsNullOrWhiteSpace(accessKey) && !string.IsNullOrWhiteSpace(secretKey)
            ? new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), config)
            : new AmazonS3Client(config);
    }
}
