using Nevma.Commands.Api.Domain;

namespace Nevma.Commands.Api.Application;

public interface ICommandRepository
{
    Task AddAsync(CommandRequest command, CancellationToken cancellationToken = default);
    Task<CommandRequest?> GetAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<CommandRequest?> FindByKeyAsync(Guid userId, string key, CancellationToken cancellationToken = default);
}
public interface ICommandsUnitOfWork { Task<int> SaveChangesAsync(CancellationToken cancellationToken = default); }
public interface IIntentParser { ParseResult Parse(string transcript, DateTimeOffset now); }
public interface ICommandExecutor
{
    Task<ExecutionResult> ExecuteAsync(CommandRequest command, string accessToken, CancellationToken cancellationToken = default);
    Task<bool> UndoAsync(CommandRequest command, string accessToken, CancellationToken cancellationToken = default);
}
public abstract record ParseResult
{
    public sealed record Parsed(ParsedCommand Command) : ParseResult;
    public sealed record Invalid(string Message) : ParseResult;
}
public sealed record ExecutionResult(bool Success, string? Resource, string? Error);
