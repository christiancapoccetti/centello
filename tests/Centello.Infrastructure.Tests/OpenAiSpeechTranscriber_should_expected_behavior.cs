using System.Net;
using System.Text;
using Centello.Application;

namespace Centello.Infrastructure.Tests;

public sealed class OpenAiSpeechTranscriber_should_expected_behavior
{
    private const string ApiKey = "sk-secret-test-key";
    private static readonly RecordedAudio Audio = new(new byte[] { 1, 2, 3, 4 });

    private static OpenAiSpeechTranscriber CreateTranscriber(FakeHttpMessageHandler handler, string apiKey = ApiKey) =>
        new(new OpenAiClient(new HttpClient(handler), new FakeApiKeyProvider(apiKey)));

    private static Task<Result<string>> Transcribe(
        FakeHttpMessageHandler handler,
        IReadOnlyCollection<string>? keywords = null,
        RecordedAudio? audio = null,
        string apiKey = ApiKey,
        CancellationToken cancellationToken = default) =>
        CreateTranscriber(handler, apiKey).Transcribe(audio ?? Audio, keywords ?? [], cancellationToken);

    private static FakeHttpMessageHandler Responding(string text = "Ho speso 100 euro di carburante") =>
        FakeHttpMessageHandler.Responding(HttpStatusCode.OK, $$"""{"text":"{{text}}"}""");

    [Fact]
    public async Task Return_the_transcribed_text()
    {
        var result = await Transcribe(Responding("  Ho speso 100 euro di carburante \\n"));

        Assert.True(result.IsSuccess);
        Assert.Equal("Ho speso 100 euro di carburante", result.Value);
    }

    [Fact]
    public async Task Post_a_multipart_form_to_the_transcriptions_endpoint_with_the_bearer_key()
    {
        var handler = Responding();

        await Transcribe(handler);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.openai.com/v1/audio/transcriptions", request.Uri!.ToString());
        Assert.Equal("Bearer", request.Authorization!.Scheme);
        Assert.Equal(ApiKey, request.Authorization.Parameter);
    }

    [Fact]
    public async Task Send_the_audio_as_a_wav_file()
    {
        var handler = Responding();

        await Transcribe(handler);

        var file = Assert.Single(handler.Requests.Single().Parts, part => part.Name == "file");
        Assert.Equal("audio.wav", file.FileName);
        Assert.Equal("audio/wav", file.MediaType);
        Assert.Equal("RIFF", Encoding.ASCII.GetString(file.Bytes, 0, 4));
        Assert.Equal(WavEncoder.Encode(Audio), file.Bytes);
    }

    [Fact]
    public async Task Select_the_transcription_model_and_json_output()
    {
        var handler = Responding();

        await Transcribe(handler);

        var request = handler.Requests.Single();
        Assert.Equal(["gpt-transcribe"], request.Values("model"));
        Assert.Equal(["json"], request.Values("response_format"));
    }

    [Fact]
    public async Task Hint_the_language_with_languages_and_never_with_language()
    {
        var handler = Responding();

        await Transcribe(handler);

        var request = handler.Requests.Single();
        Assert.Equal(["it"], request.Values("languages[]"));
        Assert.DoesNotContain(request.Parts, part => part.Name == "language");
    }

    [Fact]
    public async Task Send_one_keyword_part_per_category_and_tag_name()
    {
        var handler = Responding();

        await Transcribe(handler, ["Auto", "Carburante", "Città"]);

        Assert.Equal(["Auto", "Carburante", "Città"], handler.Requests.Single().Values("keywords[]"));
    }

    [Fact]
    public async Task Skip_blank_keywords()
    {
        var handler = Responding();

        await Transcribe(handler, ["Auto", "", "  ", "Casa"]);

        Assert.Equal(["Auto", "Casa"], handler.Requests.Single().Values("keywords[]"));
    }

    [Fact]
    public async Task Cap_the_number_of_keywords()
    {
        var handler = Responding();
        var keywords = Enumerable.Range(1, 150).Select(number => $"voce{number}").ToList();

        await Transcribe(handler, keywords);

        var sent = handler.Requests.Single().Values("keywords[]");
        Assert.Equal(100, sent.Count);
        Assert.Equal("voce1", sent[0]);
        Assert.Equal("voce100", sent[^1]);
    }

    [Fact]
    public async Task Not_call_openai_for_empty_audio()
    {
        var handler = Responding();

        var result = await Transcribe(handler, audio: new RecordedAudio(ReadOnlyMemory<byte>.Empty));

        Assert.Equal(SpeechErrors.EmptyTranscription, result.Error);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Fail_when_openai_heard_nothing(string text)
    {
        var result = await Transcribe(Responding(text));

        Assert.True(result.IsFailure);
        Assert.Equal(SpeechErrors.EmptyTranscription, result.Error);
    }

    [Theory]
    [InlineData("""{"text":null}""")]
    [InlineData("""{}""")]
    public async Task Fail_when_the_text_is_missing(string body)
    {
        var result = await Transcribe(FakeHttpMessageHandler.Responding(HttpStatusCode.OK, body));

        Assert.Equal(SpeechErrors.EmptyTranscription, result.Error);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{"text":5}""")]
    public async Task Fail_on_a_malformed_response(string body)
    {
        var result = await Transcribe(FakeHttpMessageHandler.Responding(HttpStatusCode.OK, body));

        Assert.Equal(OpenAiErrors.MalformedResponse, result.Error);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "OpenAi.InvalidApiKey")]
    [InlineData(HttpStatusCode.Forbidden, "OpenAi.InvalidApiKey")]
    [InlineData(HttpStatusCode.TooManyRequests, "OpenAi.RateLimited")]
    [InlineData(HttpStatusCode.InternalServerError, "OpenAi.RequestFailed")]
    public async Task Map_http_failures_to_user_facing_errors(HttpStatusCode status, string code)
    {
        var result = await Transcribe(FakeHttpMessageHandler.Responding(status));

        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
        Assert.DoesNotContain(ApiKey, result.Error.Message);
    }

    [Fact]
    public async Task Fail_when_the_network_is_down()
    {
        var result = await Transcribe(FakeHttpMessageHandler.Failing(new HttpRequestException("offline")));

        Assert.Equal(OpenAiErrors.Unreachable, result.Error);
    }

    [Fact]
    public async Task Fail_on_timeout()
    {
        var result = await Transcribe(FakeHttpMessageHandler.Failing(new TaskCanceledException("timeout", new TimeoutException())));

        Assert.Equal(OpenAiErrors.Unreachable, result.Error);
    }

    [Fact]
    public async Task Report_caller_cancellation_as_canceled()
    {
        var handler = FakeHttpMessageHandler.WaitingForCancellation();
        using var cancellation = new CancellationTokenSource();

        var pending = Transcribe(handler, cancellationToken: cancellation.Token);
        await cancellation.CancelAsync();
        var result = await pending;

        Assert.Equal(DraftErrors.Canceled, result.Error);
    }

    [Fact]
    public async Task Make_no_request_without_an_api_key()
    {
        var handler = Responding();

        var result = await Transcribe(handler, apiKey: "");

        Assert.Equal(DraftErrors.MissingApiKey, result.Error);
        Assert.Empty(handler.Requests);
    }
}
