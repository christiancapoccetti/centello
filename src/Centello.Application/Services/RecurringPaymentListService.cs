using Centello.Domain;

namespace Centello.Application;

/// <summary>One recurring payment as a list row, ready to bind. Strings are Italian. Detail is
/// "Svago · ogni mese · prossima il 18 dic" and leaves the category out when the title already is its name.
/// Badge is "In pausa", "Terminato" or empty.</summary>
public sealed record RecurringPaymentRow(Guid RecurringPaymentId, string Title, string Detail, string Amount,
    string IconKey, string ColorKey, string Badge)
{
    public bool HasBadge => Badge.HasValue();
    public string Description => RowLabels.Description(Title, Badge, Detail, Amount);
}

/// <summary>Every recurring payment, split by what it is doing now.</summary>
public sealed record RecurringPaymentList(IReadOnlyList<RecurringPaymentRow> Active,
    IReadOnlyList<RecurringPaymentRow> Paused, IReadOnlyList<RecurringPaymentRow> Ended)
{
    public bool HasActive => Active.Count > 0;
    public bool HasPaused => Paused.Count > 0;
    public bool HasEnded => Ended.Count > 0;
    public bool IsEmpty => !HasActive && !HasPaused && !HasEnded;

    /// <summary>"3 attivi", for the row that leads here.</summary>
    public string ActiveSummary => Active.Count switch
    {
        0 => "Nessuno attivo",
        1 => "1 attivo",
        var count => $"{count} attivi"
    };
}

public sealed class RecurringPaymentListService(ICategoryRepository categories,
    IRecurringPaymentRepository recurringPayments, Func<DateOnly>? today = null)
{
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    public async Task<Result<RecurringPaymentList>> Get(CancellationToken cancellationToken = default)
    {
        // The reads are awaited one after the other: the repositories share a scoped DbContext, which never allows
        // two operations at once.
        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<RecurringPaymentList>.Failure(categoryResult.Error);

        var paymentResult = await recurringPayments.GetAll(cancellationToken);
        if (paymentResult.IsFailure)
            return Result<RecurringPaymentList>.Failure(paymentResult.Error);

        var day = _today();
        var categoryById = categoryResult.Value.ToDictionary(category => category.CategoryId);
        var byStatus = paymentResult.Value.ToLookup(payment => payment.Status);
        IReadOnlyList<RecurringPaymentRow> Rows(RecurringPaymentStatus status, Func<RecurringPayment, DateOnly> order, bool newestFirst = false)
        {
            var ordered = newestFirst ? byStatus[status].OrderByDescending(order) : byStatus[status].OrderBy(order);
            return ordered
                .Select(payment => ToRow(payment, categoryById.GetValueOrDefault(payment.CategoryId), day))
                .ToList();
        }

        // The payment that spends next comes first. A terminated list starts from the one that ended last.
        return Result<RecurringPaymentList>.Success(new RecurringPaymentList(
            Rows(RecurringPaymentStatus.Active, payment => payment.NextDueOn),
            Rows(RecurringPaymentStatus.Paused, payment => payment.NextDueOn),
            Rows(RecurringPaymentStatus.Ended, payment => payment.EndsOn.GetValueOrDefault(), newestFirst: true)));
    }

    private static RecurringPaymentRow ToRow(RecurringPayment payment, Category? category, DateOnly today)
    {
        var label = RowLabels.Title(payment.Note, RowLabels.NameOf(category));
        var (when, badge) = payment.Status switch
        {
            RecurringPaymentStatus.Active => ($"prossima il {ItalianFormat.ShortDate(payment.NextDueOn, today)}", ""),
            RecurringPaymentStatus.Paused => ("", "In pausa"),
            _ => ($"ultima il {ItalianFormat.ShortDate(payment.EndsOn.GetValueOrDefault(), today)}", "Terminato")
        };
        var every = payment.Frequency switch
        {
            RecurrenceFrequency.Weekly => "ogni settimana",
            RecurrenceFrequency.Monthly => "ogni mese",
            _ => "ogni anno"
        };
        return new RecurringPaymentRow(payment.RecurringPaymentId, label.Title,
            RowLabels.Sentence(RowLabels.Detail(label.Category, every, when)), ItalianFormat.Money(payment.Amount.Cents),
            CategoryIcons.Resolve(category?.Icon).Key, CategoryPalette.Resolve(category?.Color).Key, badge);
    }
}
