using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Application;

/// <summary>Turns a spoken or typed phrase into the draft the expense editor opens with. Nothing is saved here.</summary>
public sealed class SpeechExpenseDraftService(
    ISpeechRecorder recorder,
    ISpeechTranscriber transcriber,
    IExpenseTextInterpreter interpreter,
    OpenAiSettings settings,
    IServiceScopeFactory scopeFactory,
    IPendingExpenseSpeechDraft pendingDraft)
{
    public bool IsListening => recorder.IsListening;
    public string LastTranscription { get; private set; } = "";

    public Task<bool> IsAvailable() => settings.HasApiKey();

    public Task<Result> Start(CancellationToken cancellationToken = default)
    {
        LastTranscription = "";
        return recorder.Start(cancellationToken);
    }

    public Task Cancel() => recorder.Cancel();

    public async Task<Result> StopAndPrepare(CancellationToken cancellationToken = default)
    {
        var audio = await recorder.Stop(cancellationToken);
        if (audio.IsFailure)
            return Result.Failure(audio.Error);

        var categories = await LoadCategories(cancellationToken);
        if (categories.IsFailure)
            return Result.Failure(categories.Error);

        var keywords = new ExpenseInterpretationCatalog(categories.Value).Keywords;
        var transcription = await transcriber.Transcribe(audio.Value, keywords, cancellationToken);
        if (transcription.IsFailure)
            return Result.Failure(transcription.Error);

        LastTranscription = transcription.Value;
        return await Interpret(transcription.Value, categories.Value, cancellationToken);
    }

    public async Task<Result> PrepareFromText(string text, CancellationToken cancellationToken = default)
    {
        var categories = await LoadCategories(cancellationToken);
        if (categories.IsFailure)
            return Result.Failure(categories.Error);

        return await Interpret(text, categories.Value, cancellationToken);
    }

    private async Task<Result> Interpret(
        string text,
        IReadOnlyCollection<SpeechCategory> categories,
        CancellationToken cancellationToken)
    {
        var draft = await interpreter.Interpret(text, categories, DateOnly.FromDateTime(DateTime.Today), cancellationToken);
        if (draft.IsFailure)
            return Result.Failure(draft.Error);

        pendingDraft.Set(draft.Value);
        return Result.Success();
    }

    private async Task<Result<IReadOnlyList<SpeechCategory>>> LoadCategories(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();
        var tagService = scope.ServiceProvider.GetRequiredService<TagService>();
        var categories = await categoryService.GetActive(cancellationToken);
        if (categories.IsFailure)
            return Result<IReadOnlyList<SpeechCategory>>.Failure(categories.Error);

        var options = new List<SpeechCategory>();
        foreach (var category in categories.Value)
        {
            var tags = await tagService.GetActive(category.CategoryId, cancellationToken);
            if (tags.IsFailure)
                return Result<IReadOnlyList<SpeechCategory>>.Failure(tags.Error);

            options.Add(new SpeechCategory(
                category.CategoryId,
                category.Name,
                [.. tags.Value.Select(tag => new SpeechTag(tag.TagId, tag.Name))]));
        }

        return Result<IReadOnlyList<SpeechCategory>>.Success(options);
    }
}
