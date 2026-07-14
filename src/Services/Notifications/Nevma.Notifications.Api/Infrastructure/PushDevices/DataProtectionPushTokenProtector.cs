using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Nevma.Notifications.Api.Application.PushDevices;

namespace Nevma.Notifications.Api.Infrastructure.PushDevices;

public sealed class DataProtectionPushTokenProtector(IDataProtectionProvider provider)
    : IPushTokenProtector
{
    private readonly IDataProtector _protector = provider.CreateProtector(
        "Nevma.Notifications.PushTokens.v1");

    public string Protect(string token) => _protector.Protect(token);

    public string Unprotect(string protectedToken) => _protector.Unprotect(protectedToken);

    public string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
