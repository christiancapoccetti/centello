using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Application.Tests;

internal sealed class FakeApiKeyStore : IOpenAiApiKeyStore
{
    public string Key { get; set; } = "";
    public Task<string> GetApiKey() => Task.FromResult(Key);
    public Task SetApiKey(string apiKey) => Task.CompletedTask;
    public void ClearApiKey() => Key = "";
}

internal sealed class FakeInternetAccess : IInternetAccess
{
    public bool IsAvailable { get; set; } = true;
}

internal sealed class FakeSpeechRecorder : ISpeechRecorder
{
    public RecordedAudio Audio { get; set; } = new(ReadOnlyMemory<byte>.Empty);
    public Result<RecordedAudio>? StopResult { get; set; }
    public int StartCount { get; private set; }
    public int StopCount { get; private set; }
    public int CancelCount { get; private set; }
    public Result? StartResult { get; set; }
    public Task? StartGate { get; set; }
    public bool IsListening => false;

    public async Task<Result> Start(CancellationToken cancellationToken = default)
    {
        StartCount++;
        if (StartGate is not null)
            await StartGate;

        return StartResult ?? Result.Success();
    }

    public Task<Result<RecordedAudio>> Stop(CancellationToken cancellationToken = default)
    {
        StopCount++;
        return Task.FromResult(StopResult ?? Result<RecordedAudio>.Success(Audio));
    }

    public Task Cancel()
    {
        CancelCount++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeSpeechTranscriber : ISpeechTranscriber
{
    public Result<string> Result { get; set; } = Result<string>.Success("");
    public RecordedAudio? Audio { get; private set; }
    public IReadOnlyCollection<string> Keywords { get; private set; } = [];
    public int CallCount { get; private set; }
    public Task? Gate { get; set; }

    public async Task<Result<string>> Transcribe(
        RecordedAudio audio,
        IReadOnlyCollection<string> keywords,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        Audio = audio;
        Keywords = keywords;
        if (Gate is not null)
            await Gate;

        return Result;
    }
}

internal sealed class FakeExpenseTextInterpreter : IExpenseTextInterpreter
{
    public ExpenseSpeechDraft Draft { get; set; } = new(null, null, "", null, "", null, "", "");
    public Error? Failure { get; set; }
    public string Text { get; private set; } = "";
    public IReadOnlyCollection<SpeechCategory> Categories { get; private set; } = [];
    public int CallCount { get; private set; }
    public Action? OnInterpret { get; set; }

    public Task<Result<ExpenseSpeechDraft>> Interpret(
        string text,
        IReadOnlyCollection<SpeechCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        CallCount++;
        OnInterpret?.Invoke();
        Text = text;
        Categories = categories;
        return Task.FromResult(Failure is null
            ? Result<ExpenseSpeechDraft>.Success(Draft)
            : Result<ExpenseSpeechDraft>.Failure(Failure));
    }
}

internal sealed class ServiceMap(CategoryService categories, TagService tags) : IServiceProvider
{
    public object? GetService(Type serviceType) =>
        serviceType == typeof(CategoryService) ? categories
        : serviceType == typeof(TagService) ? tags
        : null;
}

internal sealed class CountingScopeFactory(IServiceProvider provider) : IServiceScopeFactory
{
    public int CreatedCount { get; private set; }

    public IServiceScope CreateScope()
    {
        CreatedCount++;
        return new Scope(provider);
    }

    private sealed class Scope(IServiceProvider provider) : IServiceScope
    {
        public IServiceProvider ServiceProvider => provider;

        public void Dispose()
        {
        }
    }
}
