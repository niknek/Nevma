namespace Nevma.Contracts.Identity;

public sealed record RegisterUserRequest(
    string Email,
    string Password,
    string DisplayName,
    string? AvatarUrl);

public sealed record UserSummary(Guid Id, string DisplayName, string? AvatarUrl);
