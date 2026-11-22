using Centello.App.Theming;

namespace Centello.App.Controls.Recap;

/// <summary>One segment per card at the top of the story: the cards seen are full, the one on screen fills with the
/// time spent on it, the rest wait at 28%.</summary>
public sealed class StoryProgress : Grid
{
    private const double SegmentHeight = 3;
    private const float RestAlpha = 0.28f;

    private Grid? _share;

    public StoryProgress()
    {
        ColumnSpacing = 3;
        HeightRequest = SegmentHeight;
        InputTransparent = true;
    }

    public void Show(int index, int count, StoryInk ink, double fill)
    {
        Children.Clear();
        ColumnDefinitions.Clear();
        _share = null;
        for (var segment = 0; segment < count; segment++)
        {
            ColumnDefinitions.Add(new ColumnDefinition(GridLength.Star));
            var rest = new BoxView { Color = ink.Ink.WithAlpha(RestAlpha) };
            var cell = new Grid { Children = { rest } };
            if (segment < index)
                rest.Color = ink.Ink;

            if (segment == index)
                cell.Add(_share = Share(ink));

            this.Add(cell, segment, 0);
        }

        Fill(fill);
    }

    /// <summary>How much of the segment on screen is full, from 0 to 1.</summary>
    public void Fill(double fill)
    {
        if (_share is null)
            return;

        var share = Math.Clamp(fill, 0, 1);
        _share.ColumnDefinitions[0].Width = new GridLength(share, GridUnitType.Star);
        _share.ColumnDefinitions[1].Width = new GridLength(1 - share, GridUnitType.Star);
    }

    private static Grid Share(StoryInk ink) => new()
    {
        ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)],
        Children = { new BoxView { Color = ink.Ink } }
    };
}
