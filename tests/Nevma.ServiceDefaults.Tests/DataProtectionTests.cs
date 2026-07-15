using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nevma.ServiceDefaults.Extensions;

namespace Nevma.ServiceDefaults.Tests;

public sealed class DataProtectionTests
{
    [Fact]
    public void Configured_key_ring_is_persisted_outside_the_container_filesystem()
    {
        var path = Path.Combine(Path.GetTempPath(), $"nevma-keys-{Guid.NewGuid():N}");
        try
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["DataProtection:KeysPath"] = path
                })
                .Build();
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddNevmaDataProtection(configuration, "Nevma.Tests");
            using var provider = services.BuildServiceProvider();

            var protector = provider.GetRequiredService<IDataProtectionProvider>()
                .CreateProtector("test");
            var protectedValue = protector.Protect("sensitive-value");

            Assert.NotEqual("sensitive-value", protectedValue);
            Assert.NotEmpty(Directory.GetFiles(path, "*.xml"));
        }
        finally
        {
            if (Directory.Exists(path)) Directory.Delete(path, recursive: true);
        }
    }
}
