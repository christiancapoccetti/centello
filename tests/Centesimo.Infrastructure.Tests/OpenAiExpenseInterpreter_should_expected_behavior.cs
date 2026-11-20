using System.Net;
using Centesimo.Application;

namespace Centesimo.Infrastructure.Tests;

public sealed class OpenAiExpenseInterpreter_should_expected_behavior
{
    private const string ApiKey = "sk-secret-test-key";
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly Guid CarId = Guid.NewGuid();
    private static readonly Guid FuelId = Guid.NewGuid();
    private static readonly Guid HomeId = Guid.NewGuid();

    private static readonly DraftCategory[] Categories =
    [
        new(CarId, "Auto", [new DraftTag(FuelId, "Carburante"), new DraftTag(Guid.NewGuid(), "Tagliando")]),
        new(HomeId, "Casa", [new DraftTag(Guid.NewGuid(), "Affitto")])
    ];

    private static OpenAiExpenseInterpreter CreateInterpreter(FakeHttpMessageHandler handler, string apiKey = ApiKey) =>
        new(new OpenAiClient(new HttpClient(handler), new FakeApiKeyProvider(apiKey)));

    private static Task<Result<ExpenseDraft>> Interpret(
        FakeHttpMessageHandler handler,
        string phrase = "Ho speso 100 euro di carburante",
        string apiKey = ApiKey,
        CancellationToken cancellationToken = default) =>
        CreateInterpreter(handler, apiKey).Interpret(phrase, Categories, Today, cancellationToken);

    private static readonly byte[] PhotoBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x01, 0x02, 0x03];

    private static Task<Result<ExpenseDraft>> InterpretPhoto(FakeHttpMessageHandler handler, string apiKey = ApiKey) =>
        CreateInterpreter(handler, apiKey).Interpret(ExpensePhoto.Read(PhotoBytes).Value, Categories, Today);

    [Fact]
    public async Task Turn_a_natural_phrase_into_a_draft_with_the_users_own_ids()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK, OpenAiResponses.Interpretation(amount: "100", categoryKey: "c1", tagKey: "t1"));

        var result = await Interpret(handler);

        Assert.True(result.IsSuccess);
        Assert.Equal(10_000, result.Value.AmountCents);
        Assert.Equal(CarId, result.Value.CategoryId);
        Assert.Equal(FuelId, result.Value.TagId);
        Assert.Same(DraftSource.Voice, result.Value.Source);
        Assert.Equal("Ho speso 100 euro di carburante", result.Value.Heard);
    }

    [Fact]
    public async Task Post_the_phrase_and_the_catalog_to_the_responses_endpoint()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK, OpenAiResponses.Interpretation());

        await Interpret(handler);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.openai.com/v1/responses", request.Uri!.ToString());
        Assert.Equal(ApiKey, request.Authorization!.Parameter);
        using var json = request.Json;
        var root = json.RootElement;
        Assert.Equal("gpt-6-luna", root.GetProperty("model").GetString());
        var input = root.GetProperty("input");
        Assert.Contains("c1: Auto", input[0].GetProperty("content").GetString());
        Assert.Equal("Ho speso 100 euro di carburante", input[1].GetProperty("content").GetString());
    }

    [Fact]
    public async Task Not_trust_a_category_key_the_model_invented()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK, OpenAiResponses.Interpretation(categoryKey: "c99", tagKey: "t1"));

        var result = await Interpret(handler);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.CategoryId);
        Assert.Null(result.Value.TagId);
        Assert.Equal(10_000, result.Value.AmountCents);
    }

    [Fact]
    public async Task Leave_the_amount_empty_for_the_user_when_the_model_found_none()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK, OpenAiResponses.Interpretation(amount: "null"));

        var result = await Interpret(handler);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.AmountCents);
        Assert.Equal(CarId, result.Value.CategoryId);
    }

    [Fact]
    public async Task Still_ask_the_model_when_the_user_has_no_categories()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK, OpenAiResponses.Interpretation(categoryKey: null, tagKey: null));

        var result = await CreateInterpreter(handler).Interpret("Ho speso 5 euro", [], Today);

        Assert.True(result.IsSuccess);
        Assert.Single(handler.Requests);
        Assert.Null(result.Value.CategoryId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Not_call_openai_for_blank_phrase(string phrase)
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK, OpenAiResponses.Interpretation());

        var result = await Interpret(handler, phrase);

        Assert.True(result.IsFailure);
        Assert.Equal(SpeechErrors.EmptyTranscription, result.Error);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Fail_on_a_refusal()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK, OpenAiResponses.Refusal("No."));

        var result = await Interpret(handler);

        Assert.Equal(OpenAiErrors.MalformedResponse, result.Error);
    }

    [Fact]
    public async Task Fail_on_malformed_output()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK, OpenAiResponses.Completed("not json"));

        var result = await Interpret(handler);

        Assert.Equal(OpenAiErrors.MalformedResponse, result.Error);
    }

    [Fact]
    public async Task Fail_on_a_body_that_is_not_json()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK, "<html>Bad gateway</html>");

        var result = await Interpret(handler);

        Assert.Equal(OpenAiErrors.MalformedResponse, result.Error);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, "OpenAi.InvalidApiKey")]
    [InlineData(HttpStatusCode.TooManyRequests, "OpenAi.RateLimited")]
    [InlineData(HttpStatusCode.InternalServerError, "OpenAi.RequestFailed")]
    public async Task Map_http_failures_to_user_facing_errors(HttpStatusCode status, string code)
    {
        var handler = FakeHttpMessageHandler.Responding(status);

        var result = await Interpret(handler);

        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
        Assert.DoesNotContain(ApiKey, result.Error.Message);
    }

    [Fact]
    public async Task Fail_when_the_network_is_down()
    {
        var handler = FakeHttpMessageHandler.Failing(new HttpRequestException("offline"));

        var result = await Interpret(handler);

        Assert.Equal(OpenAiErrors.Unreachable, result.Error);
    }

    [Fact]
    public async Task Fail_on_timeout()
    {
        var handler = FakeHttpMessageHandler.Failing(new TaskCanceledException("timeout", new TimeoutException()));

        var result = await Interpret(handler);

        Assert.Equal(OpenAiErrors.Unreachable, result.Error);
    }

    [Fact]
    public async Task Report_caller_cancellation_as_canceled()
    {
        var handler = FakeHttpMessageHandler.WaitingForCancellation();
        using var cancellation = new CancellationTokenSource();

        var pending = Interpret(handler, cancellationToken: cancellation.Token);
        await cancellation.CancelAsync();
        var result = await pending;

        Assert.Equal(DraftErrors.Canceled, result.Error);
    }

    [Fact]
    public async Task Make_no_request_without_an_api_key()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK, OpenAiResponses.Interpretation());

        var result = await Interpret(handler, apiKey: "");

        Assert.Equal(DraftErrors.MissingApiKey, result.Error);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Turn_a_receipt_photo_into_a_photo_draft()
    {
        var handler = FakeHttpMessageHandler.Responding(
            HttpStatusCode.OK,
            OpenAiResponses.Interpretation(amount: "12.5", categoryKey: "c2", tagKey: null, date: "2026-11-18", note: "Conad"));

        var result = await InterpretPhoto(handler);

        Assert.True(result.IsSuccess);
        Assert.Equal(1_250, result.Value.AmountCents);
        Assert.Equal(HomeId, result.Value.CategoryId);
        Assert.Equal(new DateOnly(2026, 11, 18), result.Value.OccurredOn);
        Assert.Equal("Conad", result.Value.Note);
        Assert.Same(DraftSource.Photo, result.Value.Source);
        Assert.Equal("", result.Value.Heard);
    }

    [Fact]
    public async Task Post_the_photo_to_the_responses_endpoint()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK, OpenAiResponses.Interpretation());

        await InterpretPhoto(handler);

        var request = Assert.Single(handler.Requests);
        Assert.Equal("https://api.openai.com/v1/responses", request.Uri!.ToString());
        Assert.Equal(ApiKey, request.Authorization!.Parameter);
        using var json = request.Json;
        var input = json.RootElement.GetProperty("input");
        Assert.Contains("c1: Auto", input[0].GetProperty("content").GetString());
        var part = input[1].GetProperty("content")[0];
        Assert.Equal("input_image", part.GetProperty("type").GetString());
        Assert.StartsWith("data:image/jpeg;base64,", part.GetProperty("image_url").GetString());
    }

    [Fact]
    public async Task Open_an_empty_draft_when_the_photo_shows_no_expense()
    {
        var handler = FakeHttpMessageHandler.Responding(
            HttpStatusCode.OK,
            OpenAiResponses.Interpretation(amount: "null", categoryKey: null, tagKey: null));

        var result = await InterpretPhoto(handler);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.AmountCents);
        Assert.Null(result.Value.CategoryId);
        Assert.Null(result.Value.TagId);
        Assert.Null(result.Value.OccurredOn);
        Assert.Equal("", result.Value.Note);
        Assert.Same(DraftSource.Photo, result.Value.Source);
    }

    [Fact]
    public async Task Map_an_http_failure_on_a_photo_to_a_user_facing_error()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.TooManyRequests);

        var result = await InterpretPhoto(handler);

        Assert.True(result.IsFailure);
        Assert.Equal("OpenAi.RateLimited", result.Error.Code);
        Assert.DoesNotContain(ApiKey, result.Error.Message);
    }
}
