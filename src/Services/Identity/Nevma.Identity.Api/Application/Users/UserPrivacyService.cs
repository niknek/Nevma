using Nevma.Contracts.Identity;
using Nevma.Identity.Api.Domain.Users;

namespace Nevma.Identity.Api.Application.Users;

public sealed class UserPrivacyService(
    IUserPrivacyRepository repository,
    IUserRepository userRepository,
    IIdentityUnitOfWork unitOfWork,
    TimeProvider timeProvider)
{
    public async Task<UserSettingsResponse> GetSettingsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var settings = await repository.GetSettingsAsync(userId, cancellationToken);
        if (settings is null)
        {
            settings = UserSettings.CreateDefault(userId, timeProvider.GetUtcNow());
            await repository.AddSettingsAsync(settings, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        return ToResponse(settings);
    }

    public async Task<SettingsUpdateResult> UpdateSettingsAsync(
        Guid userId,
        UpdateUserSettingsRequest request,
        CancellationToken cancellationToken = default)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(request.TimeZoneId) || request.TimeZoneId.Length > 100)
            errors[nameof(request.TimeZoneId)] = ["Time zone is required and cannot exceed 100 characters."];
        else
        {
            try { _ = TimeZoneInfo.FindSystemTimeZoneById(request.TimeZoneId); }
            catch (TimeZoneNotFoundException) { errors[nameof(request.TimeZoneId)] = ["Time zone is unknown."]; }
            catch (InvalidTimeZoneException) { errors[nameof(request.TimeZoneId)] = ["Time zone is invalid."]; }
        }
        if (string.IsNullOrWhiteSpace(request.Locale) || request.Locale.Length > 16)
            errors[nameof(request.Locale)] = ["Locale must contain 1 to 16 characters."];
        if (errors.Count > 0)
            return new SettingsUpdateResult.Invalid(errors);

        var settings = await repository.GetSettingsAsync(userId, cancellationToken);
        if (settings is null)
        {
            settings = UserSettings.CreateDefault(userId, timeProvider.GetUtcNow());
            await repository.AddSettingsAsync(settings, cancellationToken);
        }
        settings.Update(
            request.TimeZoneId,
            request.Locale,
            request.AllowPresence,
            timeProvider.GetUtcNow());
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new SettingsUpdateResult.Updated(ToResponse(settings));
    }

    public async Task<bool> BlockAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default)
    {
        if (blockerId == blockedId || await userRepository.GetByIdAsync(blockedId, cancellationToken) is null)
            return false;
        if (await repository.GetBlockAsync(blockerId, blockedId, cancellationToken) is not null)
            return true;
        await repository.AddBlockAsync(BlockedUser.Create(blockerId, blockedId, timeProvider.GetUtcNow()), cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UnblockAsync(Guid blockerId, Guid blockedId, CancellationToken cancellationToken = default)
    {
        var block = await repository.GetBlockAsync(blockerId, blockedId, cancellationToken);
        if (block is null)
            return false;
        repository.RemoveBlock(block);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<ReportUserResult> ReportAsync(
        Guid reporterId,
        Guid reportedId,
        ReportUserRequest request,
        CancellationToken cancellationToken = default)
    {
        if (reporterId == reportedId || await userRepository.GetByIdAsync(reportedId, cancellationToken) is null)
            return new ReportUserResult.NotFound();
        if (string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Length > 100 || request.Details?.Length > 2_000)
            return new ReportUserResult.Invalid();
        var report = UserReport.Create(
            reporterId,
            reportedId,
            request.Reason,
            request.Details,
            timeProvider.GetUtcNow());
        await repository.AddReportAsync(report, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return new ReportUserResult.Accepted(report.Id);
    }

    public Task<bool> IsBlockedAsync(Guid firstUserId, Guid secondUserId, CancellationToken cancellationToken = default) =>
        repository.IsBlockedAsync(firstUserId, secondUserId, cancellationToken);

    private static UserSettingsResponse ToResponse(UserSettings settings) =>
        new(settings.TimeZoneId, settings.Locale, settings.AllowPresence, settings.UpdatedAt);
}

public abstract record SettingsUpdateResult
{
    public sealed record Updated(UserSettingsResponse Settings) : SettingsUpdateResult;
    public sealed record Invalid(IReadOnlyDictionary<string, string[]> Errors) : SettingsUpdateResult;
}

public abstract record ReportUserResult
{
    public sealed record Accepted(Guid ReportId) : ReportUserResult;
    public sealed record NotFound : ReportUserResult;
    public sealed record Invalid : ReportUserResult;
}
