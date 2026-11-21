using Centesimo.App.Theming;

namespace Centesimo.App.Controls.Recap;

/// <summary>One segment per card at the top of the story: the cards seen are full, the one on screen is half
/// full, the rest wait at 28%. It speaks as "Scheda 3 di 11".</summary>
public sealed class StoryProgress : Grid
{
    private const double SegmentHeight = 3;
    private const float RestAlpha = 0.28f;
    private const double NowShare = 0.45;

    public StoryProgress()
    {
        ColumnSpacing = 3;
        HeightRequest = SegmentHeight;
        InputTransparent = true;
    }

    public void Show(int index, int count, StoryInk ink)
    {
        Children.Clear();
        ColumnDefinitions.Clear();
        for (var segment = 0; segment < count; segment++)
        {
            ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var rest = new BoxView { Color = ink.Ink.WithAlpha(RestAlpha) };
            var cell = new Grid { Children = { rest } };
            if (segment < index)
                rest.Color = ink.Ink;

            if (segment == index)
                cell.Add(Share(ink));

            this.Add(cell, segment, 0);
        }

        SemanticProperties.SetDescription(this, $"Scheda {index + 1} di {count}");
    }

    private static View Share(StoryInk ink) => new Grid
    {
        ColumnDefinitions = [new ColumnDefinition(new GridLength(NowShare, GridUnitType.Star)), new ColumnDefinition(new GridLength(1 - NowShare, GridUnitType.Star))],
        Children = { new BoxView { Color = ink.Ink } }
    };
}
