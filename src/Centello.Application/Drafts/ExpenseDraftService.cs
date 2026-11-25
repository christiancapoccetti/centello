using Microsoft.Extensions.DependencyInjection;

namespace Centello.Application;

public sealed class ExpenseDraftService(
    ISpeechRecorder recorder,
    IExpenseInterpreter interpreter,
    IInternetAccess internet,
    DraftConsent consent,
    IPdfPageCounter pdfPages,
    IServiceScopeFactory scopeFactory,
    AiAccess access)
{
    public async Task<Result> CheckCapture(CancellationToken cancellationToken = default)
    {
        if (!consent.IsEnabled)
            return Result.Failure(DraftErrors.ConsentRequired);

        var eligibility = await access.Check(cancellationToken);
        if (eligibility.IsFailure)
            return eligibility;

        if (!internet.IsAvailable)
            return Result.Failure(DraftErrors.NoInternet);

        return Result.Success();
    }

    public async Task<Result> StartListening(CancellationToken cancellationToken = default)
    {
        var eligibility = await CheckCapture(cancellationToken);
        if (eligibility.IsFailure)
            return eligibility;

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

        var eligibility = await CheckCapture(cancellationToken);
        if (eligibility.IsFailure)
            return Result<ExpenseDraft>.Failure(eligibility.Error);

        return await Prepare(
            categories => interpreter.Interpret(audio.Value, categories, Today(), cancellationToken), cancellationToken);
    }

    public async Task<Result<ExpenseDraft>> PrepareFromDocument(ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken = default)
    {
        var document = ExpenseDocument.Read(bytes);
        if (document.IsFailure)
            return Result<ExpenseDraft>.Failure(document.Error);

        var eligibility = await CheckCapture(cancellationToken);
        if (eligibility.IsFailure)
            return Result<ExpenseDraft>.Failure(eligibility.Error);

        if (document.Value.IsPdf)
        {
            var pageCount = await pdfPages.Count(document.Value.Bytes, cancellationToken);
            if (pageCount.IsFailure)
                return Result<ExpenseDraft>.Failure(pageCount.Error);

            if (pageCount.Value > DocumentLimits.MaximumPdfPages)
                return Result<ExpenseDraft>.Failure(DraftErrors.PdfTooManyPages);
        }

        return await Prepare(
            categories => interpreter.Interpret(document.Value, categories, Today(), cancellationToken), cancellationToken);
    }

    private async Task<Result<ExpenseDraft>> Prepare(Func<IReadOnlyList<DraftCategory>, Task<Result<ExpenseDraft>>> interpret,
        CancellationToken cancellationToken)
    {
        var categories = await LoadCategories(cancellationToken);
        if (categories.IsFailure)
            return Result<ExpenseDraft>.Failure(categories.Error);

        return await access.Prepare(Interpret, cancellationToken);

        async Task<Result<ExpenseDraft>> Interpret()
        {
            if (!consent.IsEnabled)
                return Result<ExpenseDraft>.Failure(DraftErrors.ConsentRequired);

            if (!internet.IsAvailable)
                return Result<ExpenseDraft>.Failure(DraftErrors.NoInternet);

            return await interpret(categories.Value);
        }
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
