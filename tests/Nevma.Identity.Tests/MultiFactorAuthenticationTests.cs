using System.Net;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nevma.Identity.Api.Application.Authentication;
using Nevma.Identity.Api.Infrastructure.Persistence;

namespace Nevma.Identity.Tests;

public sealed class MultiFactorAuthenticationTests
{
    [Fact]
    public async Task Authenticator_setup_can_be_enabled_and_disabled_with_a_valid_code()
    {
        using var factory = new IdentityApiFactory();
        await factory.InitializeDatabaseAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var service = scope.ServiceProvider.GetRequiredService<IMultiFactorAuthenticationService>();
        var account = await CreateAccountAsync(userManager);

        var setup = await service.BeginSetupAsync(account.Id);
        var code = GenerateAuthenticatorCode(setup.Value!.SharedKey);
        var enabled = await service.EnableAsync(account.Id, code);
        var status = await service.GetStatusAsync(account.Id);

        Assert.True(setup.IsSuccess);
        Assert.StartsWith("otpauth://totp/", setup.Value!.AuthenticatorUri, StringComparison.Ordinal);
        Assert.True(enabled.IsSuccess, enabled.Error.ToString());
        Assert.Equal(10, enabled.Value!.RecoveryCodes.Count);
        Assert.Equal(10, enabled.Value.RecoveryCodes.Distinct(StringComparer.Ordinal).Count());
        Assert.True(status.Value!.IsEnabled);
        Assert.Equal(10, status.Value.RecoveryCodesLeft);

        var invalidDisable = await service.DisableAsync(account.Id, "not-a-code");
        var disabled = await service.DisableAsync(account.Id, code);
        var disabledStatus = await service.GetStatusAsync(account.Id);

        Assert.Equal(MfaError.InvalidCode, invalidDisable.Error);
        Assert.True(disabled.IsSuccess);
        Assert.False(disabledStatus.Value!.IsEnabled);
        Assert.Equal(0, disabledStatus.Value.RecoveryCodesLeft);
    }

    [Fact]
    public async Task Password_login_prompts_for_a_second_factor_when_mfa_is_enabled()
    {
        using var factory = new IdentityApiFactory();
        await factory.InitializeDatabaseAsync();
        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
            var service = scope.ServiceProvider.GetRequiredService<IMultiFactorAuthenticationService>();
            var account = await CreateAccountAsync(userManager);
            var setup = await service.BeginSetupAsync(account.Id);
            var code = GenerateAuthenticatorCode(setup.Value!.SharedKey);
            var enabled = await service.EnableAsync(account.Id, code);
            Assert.True(enabled.IsSuccess, enabled.Error.ToString());
        }

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false,
            HandleCookies = true
        });
        var loginPage = await client.GetStringAsync("/account/login?returnUrl=%2Fconnect%2Fauthorize");
        var token = WebUtility.HtmlDecode(Regex.Match(
            loginPage,
            "name=\"__RequestVerificationToken\" value=\"([^\"]+)\"").Groups[1].Value);

        var response = await client.PostAsync("/account/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Email"] = "mfa@example.com",
            ["Password"] = "Strong!Password123",
            ["ReturnUrl"] = "/connect/authorize"
        }));
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Two-step verification", html, StringComparison.Ordinal);
        Assert.Contains("autocomplete=\"one-time-code\"", html, StringComparison.Ordinal);

        await using var auditScope = factory.Services.CreateAsyncScope();
        var securityEvent = await auditScope.ServiceProvider
            .GetRequiredService<IdentityDbContext>()
            .SecurityEvents
            .SingleAsync();
        Assert.Equal("login.password", securityEvent.EventType);
        Assert.True(securityEvent.Succeeded);
        Assert.NotEqual(Guid.Empty, securityEvent.UserId);
    }

    private static async Task<IdentityAccount> CreateAccountAsync(UserManager<IdentityAccount> userManager)
    {
        var account = new IdentityAccount
        {
            Id = Guid.NewGuid(),
            UserName = "mfa@example.com",
            Email = "mfa@example.com",
            EmailConfirmed = true
        };
        var created = await userManager.CreateAsync(account, "Strong!Password123");
        Assert.True(created.Succeeded, string.Join(", ", created.Errors.Select(error => error.Description)));
        return account;
    }

    private static string GenerateAuthenticatorCode(string sharedKey)
    {
        var secret = DecodeBase32(sharedKey);
        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30);
        var hash = HMACSHA1.HashData(secret, counter);
        var offset = hash[^1] & 0x0f;
        var value = ((hash[offset] & 0x7f) << 24) |
                    (hash[offset + 1] << 16) |
                    (hash[offset + 2] << 8) |
                    hash[offset + 3];
        return (value % 1_000_000).ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
    }

    private static byte[] DecodeBase32(string value)
    {
        var output = new List<byte>();
        var buffer = 0;
        var bitsLeft = 0;
        foreach (var character in value.TrimEnd('=').ToUpperInvariant())
        {
            var index = character is >= 'A' and <= 'Z'
                ? character - 'A'
                : character - '2' + 26;
            buffer = (buffer << 5) | index;
            bitsLeft += 5;
            if (bitsLeft < 8)
                continue;

            bitsLeft -= 8;
            output.Add((byte)(buffer >> bitsLeft));
            buffer &= (1 << bitsLeft) - 1;
        }
        return output.ToArray();
    }
}
