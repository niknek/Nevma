namespace Nevma.Identity.Api.Application.Authentication;

public interface IIdentityEmailSender
{
    Task SendPasswordResetAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default);
    Task SendEmailConfirmationAsync(
        string email,
        string token,
        CancellationToken cancellationToken = default);
}
