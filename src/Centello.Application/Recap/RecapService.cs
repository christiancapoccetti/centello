namespace Centello.Application;

public interface IRecapService
{
    /// <summary>The story of a closed month, a closed year, or the year of today up to yesterday. Fails with
    /// <see cref="RecapErrors.NotAvailable"/> for the month of today, a future period, or a period with too few
    /// expenses.</summary>
    Task<Result<RecapDeck>> Get(Period period, CancellationToken cancellationToken = default);

    /// <summary>The same rule without building the deck: all the notification needs.</summary>
    Task<Result<bool>> IsAvailable(Period period, CancellationToken cancellationToken = default);
}

public static class RecapErrors
{
    public static readonly Error NotAvailable = new("Recap.NotAvailable", "Questo riepilogo non è disponibile.");
}

public sealed class RecapService(ICategoryRepository categories, ITagRepository tags, IExpenseRepository expenses,
    Func<DateOnly>? today = null) : IRecapService
{
    private readonly ICategoryRepository _categories = categories;
    private readonly ITagRepository _tags = tags;
    private readonly IExpenseRepository _expenses = expenses;
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    // The reads are awaited one after the other: the repositories share a scoped DbContext, which never allows two
    // operations at once.
    public async Task<Result<RecapDeck>> Get(Period period, CancellationToken cancellationToken = default)
    {
        if (RecapSpan.Of(period, _today()) is not { } span)
            return Result<RecapDeck>.Failure(RecapErrors.NotAvailable);

        var categoryResult = await _categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<RecapDeck>.Failure(categoryResult.Error);

        var tagResult = await _tags.GetAll(cancellationToken);
        if (tagResult.IsFailure)
            return Result<RecapDeck>.Failure(tagResult.Error);

        var expenseResult = await _expenses.GetBetween(new DateOnly(period.Start.Year - 1, 1, 1), span.Through,
            cancellationToken);
        if (expenseResult.IsFailure)
            return Result<RecapDeck>.Failure(expenseResult.Error);

        if (!span.HasEnough(expenseResult.Value))
            return Result<RecapDeck>.Failure(RecapErrors.NotAvailable);

        var scope = RecapScope.Of(span, _today(), categoryResult.Value, tagResult.Value, expenseResult.Value);
        return Result<RecapDeck>.Success(RecapDeck.Build(scope));
    }

    public async Task<Result<bool>> IsAvailable(Period period, CancellationToken cancellationToken = default)
    {
        if (RecapSpan.Of(period, _today()) is not { } span)
            return Result<bool>.Success(false);

        var expenseResult = await _expenses.GetBetween(period.Start, span.Through, cancellationToken);
        return expenseResult.IsFailure
            ? Result<bool>.Failure(expenseResult.Error)
            : Result<bool>.Success(span.HasEnough(expenseResult.Value));
    }
}
