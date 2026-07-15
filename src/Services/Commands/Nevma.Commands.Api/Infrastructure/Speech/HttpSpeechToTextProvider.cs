using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using Nevma.Commands.Api.Application;

namespace Nevma.Commands.Api.Infrastructure.Speech;

public sealed class HttpSpeechToTextProvider(
    HttpClient client,
    IOptions<SpeechProviderOptions> options,
    ILogger<HttpSpeechToTextProvider> logger) : ISpeechToTextProvider
{
    public async Task<SpeechToTextResult> TranscribeAsync(
        Stream audio,
        string contentType,
        string? language,
        CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled || !TryGetSecureEndpoint(settings.Endpoint, out var endpoint))
            return new SpeechToTextResult.Unavailable();

        using var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey);
        using var form = new MultipartFormDataContent();
        var audioContent = new StreamContent(audio);
        audioContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(audioContent, "audio", "voice-input");
        if (!string.IsNullOrWhiteSpace(language)) form.Add(new StringContent(language), "language");
        request.Content = form;

        try
        {
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Speech provider returned status {StatusCode}.", (int)response.StatusCode);
                return new SpeechToTextResult.Unavailable();
            }
            var result = await response.Content.ReadFromJsonAsync<TranscriptionResponse>(cancellationToken);
            return result is not null && !string.IsNullOrWhiteSpace(result.Transcript) && result.Transcript.Length <= 2_000
                ? new SpeechToTextResult.Transcribed(result.Transcript.Trim(), result.Confidence)
                : new SpeechToTextResult.Invalid("The speech provider returned an invalid transcript.");
        }
        catch (Exception exception) when (
            exception is HttpRequestException or System.Text.Json.JsonException ||
            exception is TaskCanceledException && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "Speech provider was unavailable.");
            return new SpeechToTextResult.Unavailable();
        }
    }

    private static bool TryGetSecureEndpoint(string? value, out Uri endpoint)
    {
        var parsed = Uri.TryCreate(value, UriKind.Absolute, out var candidate) ? candidate : null;
        endpoint = candidate!;
        return parsed is not null &&
            (parsed.Scheme == Uri.UriSchemeHttps || parsed.IsLoopback && parsed.Scheme == Uri.UriSchemeHttp);
    }

    private sealed record TranscriptionResponse(string Transcript, double? Confidence);
}
