using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App;

public sealed class SpeechExpenseDraftService(
    IOfflineSpeechRecognizer recognizer,
    IServiceScopeFactory scopeFactory,
    IExpenseTextInterpreter interpreter,
    IPendingExpenseSpeechDraft pendingDraft)
{
    public Task<Result> WarmUp(CancellationToken cancellationToken = default) =>
        recognizer.WarmUp(cancellationToken);

    public bool IsListening => recognizer.IsListening;
    public string LastTranscription { get; private set; } = "";

    public Task<Result> Start(CancellationToken cancellationToken = default)
    {
        LastTranscription = "";
        return recognizer.Start(cancellationToken);
    }
    public Task Cancel() => recognizer.Cancel();

    public async Task<Result> StopAndPrepare(CancellationToken cancellationToken = default)
    {
        var transcription = await recognizer.Stop(cancellationToken);
        if (transcription.IsFailure)
        {
            LastTranscription = "";
            return Result.Failure(transcription.Error);
        }

        LastTranscription = transcription.Value;

        var categories = await LoadCategories(cancellationToken);
        if (categories.IsFailure)
            return Result.Failure(categories.Error);

        var draft = await interpreter.Interpret(
            transcription.Value,
            categories.Value,
            DateOnly.FromDateTime(DateTime.Today),
            cancellationToken);
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

            options.Add(new SpeechCategory(category.CategoryId, category.Name,
                tags.Value.Select(tag => new SpeechTag(tag.TagId, tag.Name)).ToList()));
        }

        return Result<IReadOnlyList<SpeechCategory>>.Success(options);
    }
}
