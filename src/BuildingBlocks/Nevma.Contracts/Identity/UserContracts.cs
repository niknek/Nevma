namespace Nevma.Contracts.Identity;

public sealed record CreateUserRequest(string DisplayName, string? AvatarUrl);

public sealed record UserSummary(Guid Id, string DisplayName, string? AvatarUrl);
