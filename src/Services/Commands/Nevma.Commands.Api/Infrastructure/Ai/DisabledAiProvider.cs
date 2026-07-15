using Nevma.Commands.Api.Application;

namespace Nevma.Commands.Api.Infrastructure.Ai;

public sealed class DisabledAiProvider : IAiProvider
{
    public Task<AiInterpretationResult> InterpretAsync(
        string transcript,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<AiInterpretationResult>(new AiInterpretationResult.Unavailable());
}
