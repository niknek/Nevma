using Nevma.Commands.Api.Application;

namespace Nevma.Commands.Api.Infrastructure.Speech;

public sealed class DisabledSpeechToTextProvider : ISpeechToTextProvider
{
    public Task<SpeechToTextResult> TranscribeAsync(
        Stream audio,
        string contentType,
        string? language,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<SpeechToTextResult>(new SpeechToTextResult.Unavailable());
}
