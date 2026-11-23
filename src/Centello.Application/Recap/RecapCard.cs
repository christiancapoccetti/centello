namespace Centello.Application;

/// <summary>One card of a story. The set is closed: the constructor is private, so every kind is a nested record
/// (one partial file each, under Recap/Cards), and a renderer reaches a kind only through
/// <see cref="IRecapCardVisitor{T}"/>, so a new kind fails the build of every renderer until it draws it. Every string
/// is finished Italian; every chart is 0..1 fractions, kinds and palette keys.</summary>
public abstract partial record RecapCard
{
    private RecapCard()
    {
    }

    /// <summary>What the card is painted on: the ground, unless the card is about one category.</summary>
    public virtual RecapSurface Surface => RecapSurface.Ground;

    /// <summary>What the status bar and the navigation bar sit on. The surface, except for a card in halves
    /// (Changes) or one whose last block reaches the bottom edge (Categories).</summary>
    public virtual RecapSurface Top => Surface;

    public virtual RecapSurface Bottom => Surface;

    /// <summary>The line this card leaves on the summary card. The summary is derived from the cards present, so a
    /// card that is not shown never leaves a row.</summary>
    public RecapSummaryRow? Row { get; private init; }

    public abstract T Accept<T>(IRecapCardVisitor<T> visitor);
}

public interface IRecapCardVisitor<out T>
{
    T Cover(RecapCard.Cover card);
    T Total(RecapCard.Total card);
    T Rank(RecapCard.Rank card);
    T Months(RecapCard.Months card);
    T RecordDay(RecapCard.RecordDay card);
    T Weekday(RecapCard.Weekday card);
    T Categories(RecapCard.Categories card);
    T SpendingMap(RecapCard.SpendingMap card);
    T Changes(RecapCard.Changes card);
    T NewHabit(RecapCard.NewHabit card);
    T TagOfYear(RecapCard.TagOfYear card);
    T CostliestTag(RecapCard.CostliestTag card);
    T SteadyTag(RecapCard.SteadyTag card);
    T NewHabits(RecapCard.NewHabits card);
    T SmallExpenses(RecapCard.SmallExpenses card);
    T Automatic(RecapCard.Automatic card);
    T Consistency(RecapCard.Consistency card);
    T YearSoFar(RecapCard.YearSoFar card);
    T Summary(RecapCard.Summary card);
}
