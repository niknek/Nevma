using OpenIddict.Validation.AspNetCore;

namespace Nevma.Messaging.Api.Infrastructure.Authentication;

public static class AuthenticationConfiguration
{
    public static IServiceCollection AddMessagingAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var issuer = configuration["Authentication:Issuer"]
            ?? throw new InvalidOperationException("Authentication:Issuer is required.");

        services.AddOpenIddict()
            .AddValidation(options =>
            {
                options.SetIssuer(new Uri(issuer, UriKind.Absolute));
                options.AddAudiences("nevma_api");
                options.UseSystemNetHttp();
                options.UseAspNetCore();
            });

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
        });
        services.AddAuthorization();
        return services;
    }
}
