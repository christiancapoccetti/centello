using System.Net.Http.Headers;
using System.Text.Json;
using Centello.Application;
using Centello.Domain;

namespace Centello.Infrastructure;

public sealed class OpenAiSpeechTranscriber(OpenAiClient client) : ISpeechTranscriber
{
    private const int MaximumKeywords = 100;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<Result<string>> Transcribe(
        RecordedAudio audio,
        IReadOnlyCollection<string> keywords,
        CancellationToken cancellationToken = default)
    {
        if (audio.Pcm16.IsEmpty)
            return Result<string>.Failure(SpeechErrors.EmptyTranscription);

        using var form = BuildForm(audio, keywords);
        var response = await client.Post("audio/transcriptions", form, cancellationToken);
        if (response.IsFailure)
            return Result<string>.Failure(response.Error);

        return ParseText(response.Value);
    }

    // gpt-transcribe takes "languages[]"; sending the legacy "language" field as well would be rejected.
    private static MultipartFormDataContent BuildForm(RecordedAudio audio, IReadOnlyCollection<string> keywords)
    {
        var file = new ByteArrayContent(WavEncoder.Encode(audio));
        file.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
        var form = new MultipartFormDataContent
        {
            { file, "file", "audio.wav" },
            { new StringContent(OpenAiModels.Transcription), "model" },
            { new StringContent("it"), "languages[]" },
            { new StringContent("json"), "response_format" }
        };
        foreach (var keyword in keywords.Where(keyword => keyword.HasValue()).Take(MaximumKeywords))
            form.Add(new StringContent(keyword), "keywords[]");

        return form;
    }

    private static Result<string> ParseText(string body)
    {
        try
        {
            var text = JsonSerializer.Deserialize<TranscriptionResponse>(body, JsonOptions)?.Text;
            return text.HasValue()
                ? Result<string>.Success(text!.Trim())
                : Result<string>.Failure(SpeechErrors.EmptyTranscription);
        }
        catch (JsonException)
        {
            return Result<string>.Failure(OpenAiErrors.MalformedResponse);
        }
    }

    private sealed record TranscriptionResponse(string? Text);
}
