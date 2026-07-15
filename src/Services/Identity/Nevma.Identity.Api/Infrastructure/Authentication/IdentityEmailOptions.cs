namespace Nevma.Identity.Api.Infrastructure.Authentication;

public sealed class IdentityEmailOptions
{
    public const string SectionName = "EmailDelivery";
    public bool Enabled { get; init; }
    public string? SmtpHost { get; init; }
    public int SmtpPort { get; init; } = 587;
    public bool UseSsl { get; init; } = true;
    public string? Username { get; init; }
    public string? Password { get; init; }
    public string? FromAddress { get; init; }
    public string PasswordResetRedirectUri { get; init; } = "com.nevma.app:/account/reset-password";
    public string EmailConfirmationRedirectUri { get; init; } = "com.nevma.app:/account/confirm-email";
}
