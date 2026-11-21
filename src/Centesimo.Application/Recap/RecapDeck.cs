namespace Centesimo.Application;

/// <summary>A story: the cover, the cards with something to say in their fixed order, the summary. The constructor
/// is the invariant: nothing else can put a card before the cover or after the summary.</summary>
public sealed record RecapDeck
{
    private static readonly IReadOnlyList<Func<RecapScope, RecapCard?>> MonthCards =
    [
        RecapCard.Total.Of,
        RecapCard.Rank.Of,
        RecapCard.RecordDay.Of,
        RecapCard.Categories.Of,
        RecapCard.Changes.Of,
        RecapCard.NewHabit.Of,
        RecapCard.SmallExpenses.Of,
        RecapCard.Consistency.Of,
        RecapCard.YearSoFar.Of
    ];

    private static readonly IReadOnlyList<Func<RecapScope, RecapCard?>> YearCards =
    [
        RecapCard.Total.Of,
        RecapCard.Months.Of,
        RecapCard.RecordDay.Of,
        RecapCard.Weekday.Of,
        RecapCard.Categories.Of,
        RecapCard.Changes.Of,
        RecapCard.TagOfYear.Of,
        RecapCard.CostliestTag.Of,
        RecapCard.SteadyTag.Of,
        RecapCard.SmallExpenses.Of,
        RecapCard.Consistency.Of
    ];

    public RecapDeck(RecapSpan span, RecapCard.Cover cover, IReadOnlyList<RecapCard> middle, RecapCard.Summary summary)
    {
        Span = span;
        Title = RecapTexts.Title(span);
        Badge = RecapTexts.Badge(span);
        Cards = [cover, .. middle, summary];
    }

    public RecapSpan Span { get; }
    public string Title { get; }
    public string Badge { get; }
    public IReadOnlyList<RecapCard> Cards { get; }

    internal static RecapDeck Build(RecapScope scope)
    {
        var middle = TableOf(scope.Span.Period.Kind)
            .Select(card => card(scope))
            .OfType<RecapCard>()
            .ToList();
        return new RecapDeck(scope.Span, RecapCard.Cover.Of(scope), middle, RecapCard.Summary.Of(scope, middle));
    }

    private static IReadOnlyList<Func<RecapScope, RecapCard?>> TableOf(PeriodKind kind) => kind switch
    {
        PeriodKind.Month => MonthCards,
        PeriodKind.Year => YearCards
    };
}
