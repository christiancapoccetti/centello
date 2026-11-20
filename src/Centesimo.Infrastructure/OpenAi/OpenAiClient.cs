using System.Net.Http.Headers;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.Infrastructure;

/// <summary>
/// Sends one authenticated POST to the OpenAI API and maps every failure to a user-facing error.
/// The key is read per request and never logged or put in an error; requests are not retried.
/// </summary>
public sealed class OpenAiClient
{
    private static readonly Uri BaseAddress = new("https://api.openai.com/v1/");
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(60);
    private readonly HttpClient _http;
    private readonly IOpenAiApiKeyProvider _apiKeys;

    public OpenAiClient(HttpClient http, IOpenAiApiKeyProvider apiKeys)
    {
        http.BaseAddress = BaseAddress;
        http.Timeout = RequestTimeout;
        _http = http;
        _apiKeys = apiKeys;
    }

    /// <param name="path">Path relative to the API base address, without a leading slash.</param>
    /// <returns>The response body of a successful call.</returns>
    public async Task<Result<string>> Post(string path, HttpContent content, CancellationToken cancellationToken = default)
    {
        var apiKey = await _apiKeys.GetApiKey();
        if (apiKey.IsEmpty())
            return Result<string>.Failure(DraftErrors.MissingApiKey);

        using var request = new HttpRequestMessage(HttpMethod.Post, path) { Content = content };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        try
        {
            using var response = await _http.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return Result<string>.Failure(OpenAiErrors.FromStatus(response.StatusCode));

            return Result<string>.Success(await response.Content.ReadAsStringAsync(cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return Result<string>.Failure(DraftErrors.Canceled);
        }
        catch (Exception exception) when (exception is OperationCanceledException or HttpRequestException)
        {
            // HttpClient reports its own timeout as an OperationCanceledException that the caller did not request.
            return Result<string>.Failure(OpenAiErrors.Unreachable);
        }
    }
}
