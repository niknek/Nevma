using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;

namespace Nevma.ServiceDefaults.Extensions;

public static class DataProtectionExtensions
{
    public static IServiceCollection AddNevmaDataProtection(
        this IServiceCollection services,
        IConfiguration configuration,
        string applicationName)
    {
        var builder = services.AddDataProtection().SetApplicationName(applicationName);
        var keysPath = configuration["DataProtection:KeysPath"];
        if (!string.IsNullOrWhiteSpace(keysPath))
        {
            Directory.CreateDirectory(keysPath);
            builder.PersistKeysToFileSystem(new DirectoryInfo(keysPath));
        }

        var certificatePath = configuration["DataProtection:CertificatePath"];
        if (!string.IsNullOrWhiteSpace(certificatePath))
        {
            var certificate = X509CertificateLoader.LoadPkcs12FromFile(
                certificatePath,
                configuration["DataProtection:CertificatePassword"],
                X509KeyStorageFlags.EphemeralKeySet);
            builder.ProtectKeysWithCertificate(certificate);
        }
        return services;
    }
}
