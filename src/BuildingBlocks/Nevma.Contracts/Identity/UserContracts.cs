namespace Nevma.Contracts.Identity;

public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string DisplayName,
    string? AvatarUrl);

public sealed record UserSummary(Guid Id, string DisplayName, string? AvatarUrl);

public sealed record UpdateProfileRequest(string DisplayName, string? AvatarUrl);

public sealed record UserSettingsResponse(
    string TimeZoneId,
    string Locale,
    bool AllowPresence,
    DateTimeOffset UpdatedAt);

public sealed record UpdateUserSettingsRequest(
    string TimeZoneId,
    string Locale,
    bool AllowPresence);

public sealed record ReportUserRequest(string Reason, string? Details);
