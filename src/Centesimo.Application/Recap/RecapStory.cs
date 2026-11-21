namespace Centesimo.Application;

/// <summary>Where the person is in a deck, so the view model only forwards taps.</summary>
public sealed class RecapStory(RecapDeck deck)
{
    public RecapDeck Deck { get; } = deck;
    public int Index { get; private set; }
    public int Count => Deck.Cards.Count;
    public RecapCard Current => Deck.Cards[Index];

    /// <summary>False on the last card.</summary>
    public bool Next()
    {
        if (Index >= Count - 1)
            return false;

        Index++;
        return true;
    }

    /// <summary>False on the first card.</summary>
    public bool Previous()
    {
        if (Index == 0)
            return false;

        Index--;
        return true;
    }

    public void Restart() => Index = 0;
}
