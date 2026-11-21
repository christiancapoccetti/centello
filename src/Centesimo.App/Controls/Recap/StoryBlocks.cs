using Centesimo.App.Theming;
using Centesimo.Application;
using Microsoft.Maui.Controls.Shapes;

namespace Centesimo.App.Controls.Recap;

/// <summary>The pieces every card is made of: lines of text, the amount, chips, rows, tiles and pills. Each one takes
/// its colors from the card's ink and nothing else, so the same block reads on the ground and on a category's color.
/// The text styles are the Story ones of Typography.xaml.</summary>
internal static class StoryBlocks
{
    public const double GlyphIconSize = 22;

    public static Label Lead(string text, StoryInk ink) => Text("StoryLead", text, ink.Mute);

    public static Label Headline(string text, StoryInk ink, double size = 30)
    {
        var label = Text("StoryHeadline", text, ink.Ink);
        label.FontSize = size;
        return label;
    }

    /// <summary>The amount as the card's numeral, the euro sign smaller after a thin space.</summary>
    public static Label Amount(string amount, StoryInk ink, bool isTotal, double margin = 0)
    {
        var label = Text(isTotal ? "StoryTotal" : "StoryAmount", "", ink.Ink);
        label.Margin = new Thickness(0, -margin, 0, -margin);
        label.FormattedText = new FormattedString
        {
            Spans =
            {
                new Span { Text = amount },
                new Span { Text = " ", FontSize = 4 },
                new Span { Text = "€", FontSize = isTotal ? 40 : 30 }
            }
        };
        return label;
    }

    /// <summary>One sentence with a bold ink run, like "<b>5 volte</b>, 42,50 € in tutto".</summary>
    public static Label Phrase(RecapPhrase phrase, StoryInk ink, double size = 16)
    {
        var label = new Label
        {
            FontFamily = Fonts.Regular,
            FontSize = size,
            LineHeight = 1.4,
            TextColor = ink.Mute,
            FormattedText = new FormattedString
            {
                Spans =
                {
                    new Span { Text = phrase.Lead },
                    new Span { Text = phrase.Emphasis, FontFamily = Fonts.Bold, TextColor = ink.Ink },
                    new Span { Text = phrase.Tail }
                }
            }
        };
        return label;
    }

    public static Label Caption(string text, StoryInk ink, double size = 12) => new()
    {
        Text = text,
        FontFamily = Fonts.Regular,
        FontSize = size,
        TextColor = ink.Mute
    };

    public static BoxView Rule(StoryInk ink) => new() { Color = ink.Hairline, HeightRequest = 1 };

    /// <summary>The chip of a comparison: raised, or green when the spending went down.</summary>
    public static View Chip(RecapChip chip, StoryInk ink)
    {
        var isLower = chip.Trend == ComparisonTrend.Lower;
        var color = ThemeColors.Resolve(isLower ? ThemeColor.OnPositiveContainer : ThemeColor.Ink, ink.IsDark);
        var glyph = new Label
        {
            Text = chip.Trend == ComparisonTrend.Lower ? MaterialSymbols.SouthEast : MaterialSymbols.NorthEast,
            FontFamily = Fonts.Icons,
            FontSize = 18,
            TextColor = color,
            VerticalOptions = LayoutOptions.Center
        };
        AutomationProperties.SetIsInAccessibleTree(glyph, false);
        return new Border
        {
            StrokeThickness = 0,
            StrokeShape = new Rectangle(),
            HorizontalOptions = LayoutOptions.Start,
            HeightRequest = 26,
            Padding = new Thickness(6, 0, 10, 0),
            BackgroundColor = ThemeColors.Resolve(isLower ? ThemeColor.PositiveContainer : ThemeColor.SurfaceRaised, ink.IsDark),
            Content = new HorizontalStackLayout
            {
                Spacing = 4,
                Children =
                {
                    glyph,
                    new Label
                    {
                        Text = chip.Text,
                        FontFamily = Fonts.SemiBold,
                        FontSize = 14,
                        TextColor = color,
                        VerticalOptions = LayoutOptions.Center,
                        LineBreakMode = LineBreakMode.NoWrap
                    }
                }
            }
        };
    }

    /// <summary>The square tile of a category: its palette color under a tile-ink glyph.</summary>
    public static View Tile(RecapCategory category, double size) => new CategoryTile
    {
        IconKey = category.IconKey,
        ColorKey = category.ColorKey,
        Size = size,
        VerticalOptions = LayoutOptions.Center,
        HorizontalOptions = LayoutOptions.Start
    };

    /// <summary>The tile of the tag that is the card: on a category's color it is tile ink with the glyph in that
    /// color, so it stands out of the card; on the ground it is the category's tile.</summary>
    public static View HeroTile(RecapCategory category, StoryInk ink)
    {
        if (ink.IsGround)
            return Tile(category, 56);

        var glyph = new Label
        {
            Text = CategoryIcons.Resolve(category.IconKey).Glyph,
            FontFamily = Fonts.Icons,
            FontSize = 32,
            TextColor = ink.Palette(category.ColorKey),
            HorizontalOptions = LayoutOptions.Center,
            VerticalOptions = LayoutOptions.Center
        };
        AutomationProperties.SetIsInAccessibleTree(glyph, false);
        return new Border
        {
            WidthRequest = 56,
            HeightRequest = 56,
            StrokeThickness = 0,
            StrokeShape = new Rectangle(),
            BackgroundColor = ink.TileInk,
            VerticalOptions = LayoutOptions.Center,
            HorizontalOptions = LayoutOptions.Start,
            Content = glyph
        };
    }

    /// <summary>A tile, a title with a line of detail under it and an amount, over a hairline: an expense or a tag.
    /// A tap on it runs <paramref name="opened"/>.</summary>
    public static View Line(RecapCategory category, string title, string detail, string amount, StoryInk ink,
        Action? opened = null)
    {
        var row = new Grid
        {
            MinimumHeightRequest = 58,
            ColumnDefinitions = [new ColumnDefinition(36), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
            RowDefinitions = [new RowDefinition(GridLength.Star), new RowDefinition(1)],
            ColumnSpacing = 14
        };
        var what = new VerticalStackLayout
        {
            VerticalOptions = LayoutOptions.Center,
            Padding = new Thickness(0, 8),
            Children =
            {
                new Label { Text = title, FontFamily = Fonts.SemiBold, FontSize = 16, LineHeight = 1.25, TextColor = ink.Ink, LineBreakMode = LineBreakMode.TailTruncation },
                new Label { Text = detail, FontFamily = Fonts.Regular, FontSize = 13, TextColor = ink.Mute, LineBreakMode = LineBreakMode.TailTruncation, Margin = new Thickness(0, 2, 0, 0) }
            }
        };
        row.Add(Tile(category, 36), 0, 0);
        row.Add(what, 1, 0);
        row.Add(new Label { Text = amount, FontFamily = Fonts.Bold, FontSize = 16, TextColor = ink.Ink, VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.NoWrap }, 2, 0);
        var rule = Rule(ink);
        row.Add(rule, 0, 1);
        Grid.SetColumnSpan(rule, 3);
        if (opened is null)
            return row;

        var button = new Button
        {
            BackgroundColor = Colors.Transparent,
            Text = "",
            Padding = 0
        };
        SemanticProperties.SetDescription(button, $"{title}, {detail}, {amount}");
        SemanticProperties.SetHint(button, "Apri la spesa");
        button.Clicked += (_, _) => opened();
        row.Add(button, 0, 0);
        Grid.SetColumnSpan(button, 3);
        Grid.SetRowSpan(button, 2);
        return row;
    }

    /// <summary>A line of the summary: a glyph, what it is and its value.</summary>
    public static View SummaryRow(RecapSummaryRow row, StoryInk ink, double height)
    {
        var grid = new Grid
        {
            MinimumHeightRequest = height,
            ColumnDefinitions = [new ColumnDefinition(24), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
            RowDefinitions = [new RowDefinition(GridLength.Star), new RowDefinition(1)],
            ColumnSpacing = 14
        };
        var glyph = new Label
        {
            Text = row.Glyph,
            FontFamily = Fonts.Icons,
            FontSize = GlyphIconSize,
            TextColor = ink.Mute,
            VerticalOptions = LayoutOptions.Center
        };
        AutomationProperties.SetIsInAccessibleTree(glyph, false);
        var rule = Rule(ink);
        grid.Add(glyph, 0, 0);
        grid.Add(new Label { Text = row.Label, FontFamily = Fonts.Regular, FontSize = 15, TextColor = ink.Mute, VerticalOptions = LayoutOptions.Center }, 1, 0);
        grid.Add(new Label { Text = row.Value, FontFamily = Fonts.Bold, FontSize = 15, TextColor = ink.Ink, VerticalOptions = LayoutOptions.Center, HorizontalTextAlignment = TextAlignment.End }, 2, 0);
        grid.Add(rule, 0, 1);
        Grid.SetColumnSpan(rule, 3);
        SemanticProperties.SetDescription(grid, $"{row.Label}, {row.Value}");
        return grid;
    }

    /// <summary>The ink pill, or its outlined twin: what the summary offers to do next.</summary>
    public static Button Pill(string text, bool isFilled, StoryInk ink, Action pressed)
    {
        var button = new Button
        {
            Text = text,
            FontFamily = Fonts.Bold,
            FontSize = 16,
            HeightRequest = 48,
            CornerRadius = 24,
            Padding = new Thickness(24, 0),
            BackgroundColor = isFilled ? ink.Ink : Colors.Transparent,
            TextColor = isFilled ? ink.Ground : ink.Ink,
            BorderColor = isFilled ? Colors.Transparent : ink.Mute,
            BorderWidth = isFilled ? 0 : 1
        };
        button.Clicked += (_, _) => pressed();
        return button;
    }

    /// <summary>Squares of a key: a swatch and the word it stands for.</summary>
    public static View KeyItem(View mark, string text, StoryInk ink) => new HorizontalStackLayout
    {
        Spacing = 6,
        Children =
        {
            mark,
            new Label { Text = text, FontFamily = Fonts.Regular, FontSize = 13, TextColor = ink.Mute, VerticalOptions = LayoutOptions.Center }
        }
    };

    public static BoxView Swatch(Color color) => new()
    {
        Color = color,
        WidthRequest = 10,
        HeightRequest = 10,
        VerticalOptions = LayoutOptions.Center
    };

    private static Label Text(string styleKey, string text, Color color)
    {
        var label = new Label { Text = text, TextColor = color };
        if (Microsoft.Maui.Controls.Application.Current?.Resources.TryGetValue(styleKey, out var style) == true)
            label.Style = (Style)style;

        return label;
    }
}
