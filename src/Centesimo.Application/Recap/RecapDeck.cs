namespace Centesimo.Application;

/// <summary>A story: the cover, the cards with something to say in their fixed order, the summary. The constructor
/// is the invariant: nothing else can put a card before the cover or after the summary.</summary>
public sealed record RecapDeck
{
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

    internal static RecapDeck Build(RecapScope scope) => throw new NotImplementedException();
}
