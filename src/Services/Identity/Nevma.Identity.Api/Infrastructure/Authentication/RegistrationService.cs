using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Application.Authentication;
using Nevma.Identity.Api.Domain.Users;
using Nevma.Identity.Api.Infrastructure.Persistence;

namespace Nevma.Identity.Api.Infrastructure.Authentication;

public sealed class RegistrationService(
    UserManager<IdentityAccount> userManager,
    IdentityDbContext dbContext,
    TimeProvider timeProvider) : IRegistrationService
{
    public async Task<RegistrationResult> RegisterAsync(
        RegisterUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName))
            return RegistrationResult.Failure(nameof(request.DisplayName), "Display name is required.");

        var strategy = dbContext.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var account = new IdentityAccount
            {
                Id = Guid.NewGuid(),
                Email = request.Email.Trim(),
                UserName = request.Email.Trim()
            };

            var identityResult = await userManager.CreateAsync(account, request.Password);
            if (!identityResult.Succeeded)
            {
                return RegistrationResult.Failure(
                    "registration",
                    identityResult.Errors.Select(error => error.Description).ToArray());
            }

            var profile = User.Create(
                account.Id,
                request.DisplayName,
                request.AvatarUrl,
                timeProvider.GetUtcNow());
            await dbContext.Profiles.AddAsync(profile, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return RegistrationResult.Success(
                new UserSummary(profile.Id, profile.DisplayName, profile.AvatarUrl));
        });
    }
}
