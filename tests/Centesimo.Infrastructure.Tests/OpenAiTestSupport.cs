using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Centesimo.Application;

namespace Centesimo.Infrastructure.Tests;

internal sealed class FakeApiKeyProvider(string apiKey) : IOpenAiApiKeyProvider
{
    public Task<string> GetApiKey() => Task.FromResult(apiKey);
}

internal sealed record CapturedPart(string Name, string? FileName, string? MediaType, byte[] Bytes)
{
    public string Text => Encoding.UTF8.GetString(Bytes);
}

internal sealed record CapturedRequest(
    HttpMethod Method,
    Uri? Uri,
    AuthenticationHeaderValue? Authorization,
    string Body,
    IReadOnlyList<CapturedPart> Parts)
{
    public JsonDocument Json => JsonDocument.Parse(Body);
    public IReadOnlyList<string> Values(string name) => [.. Parts.Where(part => part.Name == name).Select(part => part.Text)];
}

/// <summary>Records every request it receives, then answers with the configured response or failure.</summary>
internal sealed class FakeHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond)
    : HttpMessageHandler
{
    public List<CapturedRequest> Requests { get; } = [];

    public static FakeHttpMessageHandler Responding(HttpStatusCode status, string body = "{}") =>
        new((_, _) => Task.FromResult(new HttpResponseMessage(status)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json")
        }));

    public static FakeHttpMessageHandler Failing(Exception exception) =>
        new((_, _) => throw exception);

    public static FakeHttpMessageHandler WaitingForCancellation() =>
        new(async (_, cancellationToken) =>
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
            throw new InvalidOperationException("Unreachable.");
        });

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Requests.Add(await Capture(request));
        return await respond(request, cancellationToken);
    }

    private static async Task<CapturedRequest> Capture(HttpRequestMessage request)
    {
        var parts = new List<CapturedPart>();
        if (request.Content is MultipartFormDataContent multipart)
            foreach (var part in multipart)
                parts.Add(new CapturedPart(
                    part.Headers.ContentDisposition?.Name?.Trim('"') ?? "",
                    part.Headers.ContentDisposition?.FileName?.Trim('"'),
                    part.Headers.ContentType?.MediaType,
                    await part.ReadAsByteArrayAsync()));

        var body = request.Content is null || parts.Count > 0 ? "" : await request.Content.ReadAsStringAsync();
        return new CapturedRequest(request.Method, request.RequestUri, request.Headers.Authorization, body, parts);
    }
}
