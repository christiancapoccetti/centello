namespace Centello.Application;

/// <summary>Where the person is in a deck and for how long, so the view model only forwards taps and the page only
/// drives time. Each card is shown for <see cref="CardDuration"/>; the last one, with the summary and its buttons,
/// fills and stays.</summary>
public sealed class RecapStory(RecapDeck deck)
{
    public static readonly TimeSpan CardDuration = TimeSpan.FromSeconds(7);

    public RecapDeck Deck { get; } = deck;
    public int Index { get; private set; }

    /// <summary>The time spent on the current card, the only owner of it.</summary>
    public TimeSpan Elapsed { get; private set; }

    public int Count => Deck.Cards.Count;
    public RecapCard Current => Deck.Cards[Index];

    /// <summary>How much of the current card's time has passed, from 0 to 1.</summary>
    public double Progress => Math.Min(1, Elapsed / CardDuration);

    /// <summary>True once the last card has had its time: nothing is left to count.</summary>
    public bool IsDone => Index == Count - 1 && Elapsed >= CardDuration;

    /// <summary>Counts time on the current card and goes on when it has had its share. True when the card changed.</summary>
    public bool Advance(TimeSpan by)
    {
        if (IsDone)
            return false;

        Elapsed += by;
        if (Elapsed < CardDuration)
            return false;

        if (Index == Count - 1)
        {
            Elapsed = CardDuration;
            return false;
        }

        return Next();
    }

    /// <summary>False on the last card.</summary>
    public bool Next()
    {
        if (Index >= Count - 1)
            return false;

        Index++;
        Elapsed = TimeSpan.Zero;
        return true;
    }

    /// <summary>False on the first card, whose time starts over all the same.</summary>
    public bool Previous()
    {
        Elapsed = TimeSpan.Zero;
        if (Index == 0)
            return false;

        Index--;
        return true;
    }

    public void Restart()
    {
        Index = 0;
        Elapsed = TimeSpan.Zero;
    }
}
