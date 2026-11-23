using Microsoft.Extensions.DependencyInjection;

namespace Centello.Application.Tests;

internal sealed class FakeDraftConsentStore : IDraftConsentStore
{
    public int AcceptedVersion { get; set; } = DraftConsent.CurrentVersion;
}

internal sealed class FakePdfPageCounter : IPdfPageCounter
{
    public int Pages { get; set; } = 1;

    public Task<Result<int>> Count(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken) =>
        Task.FromResult(Result<int>.Success(Pages));
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

internal sealed class FakeExpenseInterpreter : IExpenseInterpreter
{
    public ExpenseDraft Draft { get; set; } = new(null, null, null, null, "", DraftSource.Voice, "");
    public Error? Failure { get; set; }
    public RecordedAudio? Audio { get; private set; }
    public Task? Gate { get; set; }
    public ExpenseDocument? Document { get; private set; }
    public IReadOnlyCollection<DraftCategory> Categories { get; private set; } = [];
    public int CallCount { get; private set; }
    public Action? OnInterpret { get; set; }

    public Task<Result<ExpenseDraft>> Interpret(
        RecordedAudio audio,
        IReadOnlyCollection<DraftCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        Audio = audio;
        return Interpreted(categories);
    }

    public Task<Result<ExpenseDraft>> Interpret(
        ExpenseDocument document,
        IReadOnlyCollection<DraftCategory> categories,
        DateOnly today,
        CancellationToken cancellationToken = default)
    {
        Document = document;
        return Interpreted(categories);
    }

    private async Task<Result<ExpenseDraft>> Interpreted(IReadOnlyCollection<DraftCategory> categories)
    {
        CallCount++;
        OnInterpret?.Invoke();
        Categories = categories;
        if (Gate is not null)
            await Gate;

        return Failure is null
            ? Result<ExpenseDraft>.Success(Draft)
            : Result<ExpenseDraft>.Failure(Failure);
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
