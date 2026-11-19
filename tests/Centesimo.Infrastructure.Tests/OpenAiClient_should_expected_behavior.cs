using System.Net;
using Centesimo.Application;

namespace Centesimo.Infrastructure.Tests;

public sealed class OpenAiClient_should_expected_behavior
{
    private const string ApiKey = "sk-secret-test-key";

    private static OpenAiClient CreateClient(FakeHttpMessageHandler handler, string apiKey = ApiKey) =>
        new(new HttpClient(handler), new FakeApiKeyProvider(apiKey));

    private static Task<Result<string>> Post(OpenAiClient client, CancellationToken cancellationToken = default) =>
        client.Post("responses", new StringContent("{}"), cancellationToken);

    public static TheoryData<HttpStatusCode, string> StatusErrors => new()
    {
        { HttpStatusCode.Unauthorized, "OpenAi.InvalidApiKey" },
        { HttpStatusCode.Forbidden, "OpenAi.InvalidApiKey" },
        { HttpStatusCode.TooManyRequests, "OpenAi.RateLimited" },
        { HttpStatusCode.InternalServerError, "OpenAi.RequestFailed" },
        { HttpStatusCode.BadGateway, "OpenAi.RequestFailed" },
        { HttpStatusCode.BadRequest, "OpenAi.RequestFailed" }
    };

    [Fact]
    public async Task Return_the_body_of_a_successful_call()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK, """{"ok":true}""");

        var result = await Post(CreateClient(handler));

        Assert.True(result.IsSuccess);
        Assert.Equal("""{"ok":true}""", result.Value);
    }

    [Fact]
    public async Task Post_to_the_openai_api_with_the_key_as_a_bearer_token()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK);

        await Post(CreateClient(handler));

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("https://api.openai.com/v1/responses", request.Uri!.ToString());
        Assert.Equal("Bearer", request.Authorization!.Scheme);
        Assert.Equal(ApiKey, request.Authorization.Parameter);
    }

    [Fact]
    public void Time_out_after_thirty_seconds()
    {
        var http = new HttpClient(FakeHttpMessageHandler.Responding(HttpStatusCode.OK));

        _ = new OpenAiClient(http, new FakeApiKeyProvider(ApiKey));

        Assert.Equal(TimeSpan.FromSeconds(30), http.Timeout);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Fail_without_any_request_when_the_key_is_missing(string apiKey)
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK);

        var result = await Post(CreateClient(handler, apiKey));

        Assert.True(result.IsFailure);
        Assert.Equal(SpeechErrors.MissingApiKey, result.Error);
        Assert.Empty(handler.Requests);
    }

    [Theory]
    [MemberData(nameof(StatusErrors))]
    public async Task Map_a_failure_status_to_a_user_facing_error(HttpStatusCode status, string code)
    {
        var handler = FakeHttpMessageHandler.Responding(status, "{\"error\":{\"message\":\"Incorrect API key provided: " + ApiKey + "\"}}");

        var result = await Post(CreateClient(handler));

        Assert.True(result.IsFailure);
        Assert.Equal(code, result.Error.Code);
        Assert.DoesNotContain(ApiKey, result.Error.Message);
    }

    [Fact]
    public async Task Say_that_openai_is_unreachable_when_the_network_fails()
    {
        var handler = FakeHttpMessageHandler.Failing(new HttpRequestException($"No connection while sending {ApiKey}"));

        var result = await Post(CreateClient(handler));

        Assert.Equal(OpenAiErrors.Unreachable, result.Error);
        Assert.DoesNotContain(ApiKey, result.Error.Message);
    }

    [Fact]
    public async Task Treat_an_http_client_timeout_as_unreachable_not_as_cancellation()
    {
        var handler = FakeHttpMessageHandler.Failing(new TaskCanceledException("The request was canceled due to the configured timeout.", new TimeoutException()));

        var result = await Post(CreateClient(handler));

        Assert.Equal(OpenAiErrors.Unreachable, result.Error);
    }

    [Fact]
    public async Task Report_cancellation_by_the_caller_as_canceled()
    {
        var handler = FakeHttpMessageHandler.WaitingForCancellation();
        using var cancellation = new CancellationTokenSource();

        var pending = Post(CreateClient(handler), cancellation.Token);
        await cancellation.CancelAsync();
        var result = await pending;

        Assert.True(result.IsFailure);
        Assert.Equal(SpeechErrors.Canceled, result.Error);
    }

    [Fact]
    public async Task Report_an_already_canceled_token_as_canceled()
    {
        var handler = FakeHttpMessageHandler.WaitingForCancellation();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        var result = await Post(CreateClient(handler), cancellation.Token);

        Assert.Equal(SpeechErrors.Canceled, result.Error);
    }

    [Fact]
    public async Task Not_retry_a_failed_call()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.InternalServerError);

        await Post(CreateClient(handler));

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Read_the_key_on_every_call_so_a_changed_key_applies_immediately()
    {
        var handler = FakeHttpMessageHandler.Responding(HttpStatusCode.OK);
        var keys = new MutableApiKeyProvider("sk-first");
        var client = new OpenAiClient(new HttpClient(handler), keys);

        await Post(client);
        keys.ApiKey = "sk-second";
        await Post(client);

        Assert.Equal(["sk-first", "sk-second"], handler.Requests.Select(request => request.Authorization!.Parameter));
    }

    private sealed class MutableApiKeyProvider(string apiKey) : IOpenAiApiKeyProvider
    {
        public string ApiKey { get; set; } = apiKey;
        public Task<string> GetApiKey() => Task.FromResult(ApiKey);
    }
}
