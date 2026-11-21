namespace Centesimo.Application;

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

    public Task<Result<RecapDeck>> Get(Period period, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();

    public Task<Result<bool>> IsAvailable(Period period, CancellationToken cancellationToken = default) =>
        throw new NotImplementedException();
}
