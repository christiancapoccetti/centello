using Microsoft.Extensions.DependencyInjection;

namespace Centello.Application;

public sealed class ExpenseDraftService(
    ISpeechRecorder recorder,
    ISpeechTranscriber transcriber,
    IExpenseInterpreter interpreter,
    IInternetAccess internet,
    IServiceScopeFactory scopeFactory)
{
    public bool IsListening => recorder.IsListening;
    public string LastTranscription { get; private set; } = "";

    public async Task<Result> StartListening(CancellationToken cancellationToken = default)
    {
        LastTranscription = "";
        if (!internet.IsAvailable)
            return Result.Failure(DraftErrors.NoInternet);

        return await recorder.Start(cancellationToken);
    }

    public Task CancelListening() => recorder.Cancel();

    public async Task<Result<ExpenseDraft>> PrepareFromSpeech(WhenFull whenFull, CancellationToken cancellationToken = default)
    {
        var audio = await recorder.Stop(cancellationToken);
        if (audio.IsFailure)
            return Result<ExpenseDraft>.Failure(audio.Error);

        if (audio.Value.IsFull && whenFull == WhenFull.Refuse)
            return Result<ExpenseDraft>.Failure(SpeechErrors.TooLong);

        if (!internet.IsAvailable)
            return Result<ExpenseDraft>.Failure(DraftErrors.NoInternet);

        var categories = await LoadCategories(cancellationToken);
        if (categories.IsFailure)
            return Result<ExpenseDraft>.Failure(categories.Error);

        var keywords = new ExpenseInterpretationCatalog(categories.Value).Keywords;
        var transcription = await transcriber.Transcribe(audio.Value, keywords, cancellationToken);
        if (transcription.IsFailure)
            return Result<ExpenseDraft>.Failure(transcription.Error);

        LastTranscription = transcription.Value;
        return await interpreter.Interpret(
            transcription.Value,
            categories.Value,
            Today(),
            cancellationToken);
    }

    public async Task<Result<ExpenseDraft>> PrepareFromDocument(ReadOnlyMemory<byte> image, CancellationToken cancellationToken = default)
    {
        var photo = ExpenseDocument.Read(image);
        if (photo.IsFailure)
            return Result<ExpenseDraft>.Failure(photo.Error);

        if (!internet.IsAvailable)
            return Result<ExpenseDraft>.Failure(DraftErrors.NoInternet);

        var categories = await LoadCategories(cancellationToken);
        if (categories.IsFailure)
            return Result<ExpenseDraft>.Failure(categories.Error);

        return await interpreter.Interpret(photo.Value, categories.Value, Today(), cancellationToken);
    }

    private async Task<Result<IReadOnlyList<DraftCategory>>> LoadCategories(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var categoryService = scope.ServiceProvider.GetRequiredService<CategoryService>();
        var tagService = scope.ServiceProvider.GetRequiredService<TagService>();
        var categories = await categoryService.GetActive(cancellationToken);
        if (categories.IsFailure)
            return Result<IReadOnlyList<DraftCategory>>.Failure(categories.Error);

        var options = new List<DraftCategory>();
        foreach (var category in categories.Value)
        {
            var tags = await tagService.GetActive(category.CategoryId, cancellationToken);
            if (tags.IsFailure)
                return Result<IReadOnlyList<DraftCategory>>.Failure(tags.Error);

            options.Add(new DraftCategory(
                category.CategoryId,
                category.Name,
                [.. tags.Value.Select(tag => new DraftTag(tag.TagId, tag.Name))]));
        }

        return Result<IReadOnlyList<DraftCategory>>.Success(options);
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.Today);
}
