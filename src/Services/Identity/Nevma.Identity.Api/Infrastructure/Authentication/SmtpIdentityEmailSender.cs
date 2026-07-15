using System.Net;
using System.Net.Mail;
using Nevma.Identity.Api.Application.Authentication;

namespace Nevma.Identity.Api.Infrastructure.Authentication;

public sealed class SmtpIdentityEmailSender(IdentityEmailOptions options) : IIdentityEmailSender
{
    public Task SendPasswordResetAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            email,
            "Reset your Nevma password",
            "Use this link to choose a new Nevma password:",
            options.PasswordResetRedirectUri,
            token,
            cancellationToken);

    public Task SendEmailConfirmationAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default) =>
        SendAsync(
            email,
            "Confirm your Nevma email",
            "Use this link to confirm your Nevma email address:",
            options.EmailConfirmationRedirectUri,
            token,
            cancellationToken);

    private async Task SendAsync(
        string email,
        string subject,
        string introduction,
        string redirectUri,
        string token,
        CancellationToken cancellationToken)
    {
        if (!options.Enabled)
            return;
        if (string.IsNullOrWhiteSpace(options.SmtpHost) ||
            string.IsNullOrWhiteSpace(options.FromAddress))
        {
            throw new InvalidOperationException(
                "EmailDelivery:SmtpHost and EmailDelivery:FromAddress are required when email is enabled.");
        }

        var link = $"{redirectUri}?email={Uri.EscapeDataString(email)}&token={Uri.EscapeDataString(token)}";
        using var message = new MailMessage(options.FromAddress, email, subject, $"{introduction}\n\n{link}");
        using var client = new SmtpClient(options.SmtpHost, options.SmtpPort)
        {
            EnableSsl = options.UseSsl
        };
        if (!string.IsNullOrWhiteSpace(options.Username))
            client.Credentials = new NetworkCredential(options.Username, options.Password);

        cancellationToken.ThrowIfCancellationRequested();
        await client.SendMailAsync(message, cancellationToken);
    }
}
