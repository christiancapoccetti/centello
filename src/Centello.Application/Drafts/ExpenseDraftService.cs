using Microsoft.Extensions.DependencyInjection;

namespace Centello.Application;

public sealed class ExpenseDraftService(
    ISpeechRecorder recorder,
    IExpenseInterpreter interpreter,
    IInternetAccess internet,
    DraftConsent consent,
    IPdfPageCounter pdfPages,
    IServiceScopeFactory scopeFactory)
{
    public async Task<Result> StartListening(CancellationToken cancellationToken = default)
    {
        if (!consent.IsEnabled)
            return Result.Failure(DraftErrors.ConsentRequired);

        if (!internet.IsAvailable)
            return Result.Failure(DraftErrors.NoInternet);

        return await recorder.Start(cancellationToken);
    }

    public Task CancelListening() => recorder.Cancel();

    public async Task<Result<ExpenseDraft>> PrepareFromSpeech(CancellationToken cancellationToken = default)
    {
        // Always release the microphone, even when the page leaves before preparation can start.
        var audio = await recorder.Stop();
        cancellationToken.ThrowIfCancellationRequested();
        if (audio.IsFailure)
            return Result<ExpenseDraft>.Failure(audio.Error);

        if (!consent.IsEnabled)
            return Result<ExpenseDraft>.Failure(DraftErrors.ConsentRequired);

        if (!internet.IsAvailable)
            return Result<ExpenseDraft>.Failure(DraftErrors.NoInternet);

        var categories = await LoadCategories(cancellationToken);
        if (categories.IsFailure)
            return Result<ExpenseDraft>.Failure(categories.Error);

        return await interpreter.Interpret(audio.Value, categories.Value, Today(), cancellationToken);
    }

    public async Task<Result<ExpenseDraft>> PrepareFromDocument(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        var document = ExpenseDocument.Read(bytes);
        if (document.IsFailure)
            return Result<ExpenseDraft>.Failure(document.Error);

        if (!consent.IsEnabled)
            return Result<ExpenseDraft>.Failure(DraftErrors.ConsentRequired);

        if (!internet.IsAvailable)
            return Result<ExpenseDraft>.Failure(DraftErrors.NoInternet);

        if (document.Value.IsPdf)
        {
            var pageCount = await pdfPages.Count(document.Value.Bytes, cancellationToken);
            if (pageCount.IsFailure)
                return Result<ExpenseDraft>.Failure(pageCount.Error);

            if (pageCount.Value > DocumentLimits.MaximumPdfPages)
                return Result<ExpenseDraft>.Failure(DraftErrors.PdfTooManyPages);
        }

        var categories = await LoadCategories(cancellationToken);
        if (categories.IsFailure)
            return Result<ExpenseDraft>.Failure(categories.Error);

        return await interpreter.Interpret(document.Value, categories.Value, Today(), cancellationToken);
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
