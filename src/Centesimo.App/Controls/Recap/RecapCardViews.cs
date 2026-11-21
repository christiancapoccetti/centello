using Centesimo.App.Theming;
using Centesimo.Application;

namespace Centesimo.App.Controls.Recap;

/// <summary>Draws each kind of card as a full-bleed view. The visitor is how the build learns about a new kind: it
/// fails here until the kind has a layout. A card paints its own surface to the edges of the screen and pads its
/// content into the safe area and below the story's top bar, so the page only has to put the view on screen. Buttons
/// inside a card keep their own taps; everything else lets the page's tap through.</summary>
internal sealed class RecapCardViews(bool isDark, double width, Action<Guid> openExpense, Action<RecapExitTarget> exit,
    Action restart) : IRecapCardVisitor<View>
{
    /// <summary>The story's top bar: 6 above the segments, 3 for them, the 48 high title row.</summary>
    public const double TopBar = 57;
    private const double Gutter = 24;
    private const double Rise = 40;
    private const double Foot = 28;

    private double Content => width - 2 * Gutter;

    public View Cover(RecapCard.Cover card)
    {
        var ink = Ink(RecapSurface.Ground);
        var isYear = card.Chart.Bars.Count <= 12;
        var title = StoryBlocks.Headline(card.Title, ink, isYear ? 56 : 48);
        title.LineHeight = 1.02;
        var hint = new HorizontalStackLayout
        {
            Spacing = 6,
            Margin = new Thickness(0, 18, 0, 22),
            Children = { Glyph(MaterialSymbols.TouchApp, ink, 18), StoryBlocks.Caption(card.Hint, ink) }
        };
        return Card(ink, null, Stack(
            title,
            Above(StoryBlocks.Phrase(new RecapPhrase(card.Subtitle), ink), 8),
            Above(new RecapBarsView(card.Chart, ink, isYear ? 100 : 120), Rise),
            hint));
    }

    public View Total(RecapCard.Total card)
    {
        var ink = Ink(RecapSurface.Ground);
        var chips = new VerticalStackLayout { Spacing = 8, Margin = new Thickness(0, 20, 0, 0) };
        foreach (var chip in card.Chips)
            chips.Add(StoryBlocks.Chip(chip, ink));

        var measures = new VerticalStackLayout { Spacing = 6, Margin = new Thickness(0, 0, 0, Foot) };
        foreach (var measure in card.Measures)
            measures.Add(Measure(measure, ink));

        return Card(ink,
            Stack(
                Above(StoryBlocks.Lead(card.Lead, ink), Rise),
                Above(StoryBlocks.Amount(card.Amount, ink, isTotal: true, margin: 6), 8),
                Above(StoryBlocks.Phrase(card.Detail, ink), 8),
                chips),
            measures);
    }

    public View Rank(RecapCard.Rank card) => Verdict(card.Headline, new RecapPhrase(card.Detail), card.Chart, 232);

    public View Months(RecapCard.Months card) => Verdict(card.Headline, new RecapPhrase(card.Detail), card.Chart, 232);

    public View Weekday(RecapCard.Weekday card) => Verdict(card.Headline, card.Detail, card.Days, 220);

    public View RecordDay(RecapCard.RecordDay card)
    {
        var ink = Ink(RecapSurface.Ground);
        var top = Stack(
            Above(StoryBlocks.Lead(card.Lead, ink), Rise),
            Above(StoryBlocks.Amount(card.Amount, ink, isTotal: false, margin: 4), 8),
            Above(StoryBlocks.Phrase(card.Detail, ink), 8));
        var expenses = new VerticalStackLayout { Margin = new Thickness(0, 14, 0, 0) };
        foreach (var expense in card.Expenses)
            expenses.Add(StoryBlocks.Line(expense.Category, expense.Category.Name, expense.Note, expense.Amount, ink,
                () => openExpense(expense.ExpenseId)));

        var note = Above(StoryBlocks.Caption(card.Note, ink), 10);
        if (card.Days is null)
            return Card(ink, top, Stack(expenses, note, Space(Foot - 10)));

        top.Add(Above(new RecapBarsView(card.Days, ink, 72), 26));
        top.Add(expenses);
        top.Add(note);
        return Card(ink, top, null);
    }

    public View Categories(RecapCard.Categories card)
    {
        var ground = Ink(RecapSurface.Ground);
        var root = Surface(ground);
        var inner = new Grid
        {
            SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container, SafeAreaRegions.Container, SafeAreaRegions.Container, SafeAreaRegions.None),
            Padding = new Thickness(0, TopBar, 0, 0),
            RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star)]
        };
        var lead = StoryBlocks.Phrase(card.Lead, ground, 17);
        lead.Margin = new Thickness(Gutter, 28, Gutter, 18);
        inner.Add(lead, 0, 0);
        inner.Add(Stacked(card.Shares), 0, 1);
        root.Add(inner);
        return root;
    }

    public View Changes(RecapCard.Changes card)
    {
        var halves = new Grid { RowSpacing = 0, SafeAreaEdges = SafeAreaEdges.None };
        if (card.Rise is { } rise)
            halves.AddRowDefinition(new RowDefinition(GridLength.Star));

        if (card.Fall is { } fall)
            halves.AddRowDefinition(new RowDefinition(GridLength.Star));

        var row = 0;
        if (card.Rise is not null)
            halves.Add(Half(card.Rise, isUpper: true), 0, row++);

        if (card.Fall is not null)
            halves.Add(Half(card.Fall, isUpper: card.Rise is null), 0, row);

        return halves;
    }

    public View NewHabit(RecapCard.NewHabit card)
    {
        var ink = Ink(card.Surface);
        var days = new Grid { ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star)] };
        days.Add(StoryBlocks.Caption(card.From, ink), 0, 0);
        var to = StoryBlocks.Caption(card.To, ink);
        to.HorizontalOptions = LayoutOptions.End;
        days.Add(to, 1, 0);
        days.Margin = new Thickness(0, 12, 0, Foot);
        return Card(ink, Hero(card.Category, card.Lead, card.Tag, card.Detail, 40, ink),
            Stack(new RecapBarsView(card.Days, ink, 64), days));
    }

    public View TagOfYear(RecapCard.TagOfYear card) => Tag(card.Category, card.Lead, card.Tag, card.Detail, card.Chart, card.Surface);

    public View CostliestTag(RecapCard.CostliestTag card) =>
        Tag(card.Category, card.Lead, card.Tag, card.Detail, card.Chart, card.Surface);

    public View SteadyTag(RecapCard.SteadyTag card)
    {
        var ink = Ink(card.Surface);
        var labels = new Grid
        {
            Margin = new Thickness(0, 8, 0, Foot),
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)]
        };
        labels.Add(StoryBlocks.Caption(card.Start, ink), 0, 0);
        labels.Add(StoryBlocks.Caption(card.Middle, ink), 1, 0);
        var end = StoryBlocks.Caption(card.End, ink);
        end.HorizontalOptions = LayoutOptions.End;
        labels.Add(end, 2, 0);
        return Card(ink, Hero(card.Category, card.Lead, card.Tag, card.Detail, 44, ink),
            Stack(new RecapBarsView(card.Weeks, ink, 96), labels));
    }

    public View NewHabits(RecapCard.NewHabits card)
    {
        var ink = Ink(RecapSurface.Ground);
        var habits = new VerticalStackLayout { Margin = new Thickness(0, 0, 0, Foot) };
        foreach (var habit in card.Habits)
            habits.Add(StoryBlocks.Line(habit.Category, habit.Tag, habit.Detail, habit.Amount, ink));

        return Card(ink,
            Stack(Above(StoryBlocks.Headline(card.Headline, ink), Rise), Above(StoryBlocks.Phrase(new RecapPhrase(card.Detail), ink), 8)),
            habits);
    }

    public View SmallExpenses(RecapCard.SmallExpenses card)
    {
        var ink = Ink(RecapSurface.Ground);
        var isDense = card.Units.ColorKeys.Count > 100;
        var units = new RecapUnitsView(card.Units, ink, Content, 300, isDense ? 8 : 33);
        var top = Stack(
            Above(StoryBlocks.Lead(card.Lead, ink), Rise),
            Above(StoryBlocks.Amount(card.Amount, ink, isTotal: false, margin: 4), 8),
            Above(StoryBlocks.Phrase(card.Detail, ink), 8));
        if (card.TopTag is not { } tag)
            return Card(ink, top, Stack(units, Space(Foot + 2)));

        top.Add(Above(units, 28));
        var line = StoryBlocks.Line(tag.Category, tag.Tag, tag.Detail, tag.Amount, ink);
        line.Margin = new Thickness(0, 0, 0, 26 - 2);
        return Card(ink, top, Stack(StoryBlocks.Rule(ink), line));
    }

    public View Automatic(RecapCard.Automatic card)
    {
        var ink = Ink(RecapSurface.Ground);
        var automatic = Math.Clamp(card.Split.Automatic, 0.0001, 0.9999);
        var split = new Grid
        {
            Margin = new Thickness(0, 22, 0, 0),
            HeightRequest = 28,
            ColumnSpacing = 3,
            ColumnDefinitions = [new ColumnDefinition(new GridLength(automatic, GridUnitType.Star)), new ColumnDefinition(new GridLength(1 - automatic, GridUnitType.Star))]
        };
        split.Add(new BoxView { Color = ink.Mute }, 0, 0);
        split.Add(new BoxView { Color = ink.Bar }, 1, 0);
        var key = new HorizontalStackLayout
        {
            Spacing = 16,
            Margin = new Thickness(0, 10, 0, 0),
            Children =
            {
                StoryBlocks.KeyItem(StoryBlocks.Swatch(ink.Mute), card.Split.AutomaticName, ink),
                StoryBlocks.KeyItem(StoryBlocks.Swatch(ink.Bar), card.Split.VariableName, ink)
            }
        };
        var payments = new VerticalStackLayout();
        foreach (var payment in card.Payments)
            payments.Add(PaymentRow(payment, ink));

        return Card(ink,
            Stack(
                Above(StoryBlocks.Headline(card.Headline, ink), Rise),
                Above(StoryBlocks.Phrase(card.Detail, ink), 8),
                split,
                key),
            Stack(payments, Above(StoryBlocks.Phrase(card.Note, ink, 12), 10), Space(Foot - 2)));
    }

    public View Consistency(RecapCard.Consistency card)
    {
        var ink = Ink(RecapSurface.Ground);
        var isCalendar = card.Days.Columns <= 7;
        var headline = StoryBlocks.Headline(card.Headline, ink, isCalendar ? 38 : 34);
        headline.LineBreakMode = LineBreakMode.NoWrap;
        return Card(ink,
            Stack(Above(StoryBlocks.Lead(card.Lead, ink), Rise), Above(headline, 6)),
            Stack(new RecapGridView(card.Days, ink, Content), Above(StoryBlocks.Caption(card.Note, ink), 12), Space(Foot)));
    }

    public View YearSoFar(RecapCard.YearSoFar card)
    {
        var ink = Ink(RecapSurface.Ground);
        var top = Stack(
            Above(StoryBlocks.Lead(card.Lead, ink), Rise),
            Above(StoryBlocks.Amount(card.Amount, ink, isTotal: false, margin: 4), 8));
        if (card.Chip is { } chip)
            top.Add(Above(StoryBlocks.Chip(chip, ink), 14));

        var dashes = new HorizontalStackLayout { Spacing = 4, VerticalOptions = LayoutOptions.Center, Children = { Dash(ink), Dash(ink) } };
        var key = new HorizontalStackLayout
        {
            Spacing = 16,
            Margin = new Thickness(0, 12, 0, Foot),
            Children =
            {
                StoryBlocks.KeyItem(StoryBlocks.Swatch(ink.Bar), card.ThisYear, ink),
                new HorizontalStackLayout
                {
                    Spacing = 6,
                    Children = { dashes, StoryBlocks.Phrase(card.LastYear, ink, 13) }
                }
            }
        };
        return Card(ink, top, Stack(new RecapBarsView(card.Chart, ink, 200), key));
    }

    public View Summary(RecapCard.Summary card)
    {
        var ink = Ink(RecapSurface.Ground);
        var height = card.Rows.Count > 8 ? 46 : 52;
        var rows = new VerticalStackLayout { Margin = new Thickness(0, 14, 0, 0) };
        foreach (var row in card.Rows)
            rows.Add(StoryBlocks.SummaryRow(row, ink, height));

        var pills = new VerticalStackLayout
        {
            Spacing = 12,
            Margin = new Thickness(0, 0, 0, 24),
            Children =
            {
                StoryBlocks.Pill(card.Exit.Text, true, ink, () => exit(card.Exit.Target)),
                StoryBlocks.Pill(card.Restart, false, ink, restart)
            }
        };
        return Card(ink, Stack(Above(StoryBlocks.Headline(card.Headline, ink), 28), rows), pills);
    }

    private StoryInk Ink(RecapSurface surface) => StoryInk.Of(surface, isDark);

    private View Verdict(string headline, RecapPhrase detail, RecapBars chart, double plot)
    {
        var ink = Ink(RecapSurface.Ground);
        return Card(ink,
            Stack(Above(StoryBlocks.Headline(headline, ink), Rise), Above(StoryBlocks.Phrase(detail, ink), 8)),
            Stack(new RecapBarsView(chart, ink, plot), Space(Foot - 10)));
    }

    private View Tag(RecapCategory category, string lead, string tag, RecapPhrase detail, RecapBars chart, RecapSurface surface)
    {
        var ink = Ink(surface);
        return Card(ink, Hero(category, lead, tag, detail, 44, ink), Stack(new RecapBarsView(chart, ink, 88), Space(Foot)));
    }

    private static View Hero(RecapCategory category, string lead, string tag, RecapPhrase detail, double size, StoryInk ink)
    {
        var name = new Grid
        {
            Margin = new Thickness(0, 18, 0, 0),
            ColumnSpacing = 14,
            ColumnDefinitions = [new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star)]
        };
        name.Add(StoryBlocks.HeroTile(category, ink), 0, 0);
        var label = StoryBlocks.Headline(tag, ink, size);
        label.VerticalOptions = LayoutOptions.Center;
        name.Add(label, 1, 0);
        return Stack(Above(StoryBlocks.Lead(lead, ink), Rise), name, Above(StoryBlocks.Phrase(detail, ink), 16));
    }

    private static View Measure(RecapMeasure measure, StoryInk ink)
    {
        var width = Math.Clamp(measure.Width, 0.0001, 1);
        var grid = new Grid
        {
            RowSpacing = 6,
            ColumnSpacing = 12,
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
            Margin = new Thickness(0, 16, 0, 0)
        };
        var bar = new Grid
        {
            ColumnDefinitions = [new ColumnDefinition(new GridLength(width, GridUnitType.Star)), new ColumnDefinition(new GridLength(Math.Max(1 - width, 0.0001), GridUnitType.Star))],
            HeightRequest = 20,
            VerticalOptions = LayoutOptions.Center
        };
        bar.Add(new BoxView { Color = measure.IsCurrent ? ink.Ink : ink.Bar }, 0, 0);
        grid.AddRowDefinition(new RowDefinition(GridLength.Auto));
        grid.AddRowDefinition(new RowDefinition(GridLength.Auto));
        var label = StoryBlocks.Caption(measure.Label, ink, 13);
        grid.Add(label, 0, 0);
        Grid.SetColumnSpan(label, 2);
        grid.Add(bar, 0, 1);
        grid.Add(new Label { Text = measure.Amount, FontFamily = Fonts.Bold, FontSize = 14, TextColor = ink.Ink, VerticalOptions = LayoutOptions.Center }, 1, 1);
        return grid;
    }

    private static View PaymentRow(RecapLine line, StoryInk ink)
    {
        var grid = new Grid
        {
            MinimumHeightRequest = 36,
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)],
            RowDefinitions = [new RowDefinition(GridLength.Star), new RowDefinition(1)]
        };
        grid.Add(new Label { Text = line.Name, FontFamily = Fonts.Regular, FontSize = 14, TextColor = ink.Ink, VerticalOptions = LayoutOptions.Center }, 0, 0);
        grid.Add(new Label { Text = line.Amount, FontFamily = Fonts.Bold, FontSize = 14, TextColor = ink.Ink, VerticalOptions = LayoutOptions.Center }, 1, 0);
        var rule = StoryBlocks.Rule(ink);
        grid.Add(rule, 0, 1);
        Grid.SetColumnSpan(rule, 2);
        return grid;
    }

    /// <summary>The stack of categories: every block as tall as its share, 3 apart, text only where it fits.</summary>
    private View Stacked(IReadOnlyList<RecapShare> shares)
    {
        var stack = new Grid { RowSpacing = 0, SafeAreaEdges = SafeAreaEdges.None };
        for (var index = 0; index < shares.Count; index++)
        {
            if (index > 0)
                stack.AddRowDefinition(new RowDefinition(3));

            stack.AddRowDefinition(new RowDefinition(new GridLength(Math.Max(shares[index].Fraction, 0.0001), GridUnitType.Star)));
        }

        for (var index = 0; index < shares.Count; index++)
            stack.Add(ShareBlock(shares[index]), 0, index * 2);

        return stack;
    }

    private View ShareBlock(RecapShare share)
    {
        var ink = Ink(RecapSurface.Block(share.Category.ColorKey));
        var content = new Grid
        {
            Padding = new Thickness(Gutter, 10, Gutter, 0),
            VerticalOptions = LayoutOptions.Start,
            ColumnDefinitions = [new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto)]
        };
        var name = new HorizontalStackLayout { Spacing = 8 };
        name.Add(Glyph(CategoryIcons.Resolve(share.Category.IconKey).Glyph, ink, 20, ink.Ink));
        name.Add(new Label { Text = share.Category.Name, FontFamily = Fonts.Bold, FontSize = 16, TextColor = ink.Ink, VerticalOptions = LayoutOptions.Center, LineBreakMode = LineBreakMode.TailTruncation });
        var amount = new HorizontalStackLayout { Spacing = 8 };
        amount.Add(new Label { Text = share.Amount, FontFamily = Fonts.Bold, FontSize = 16, TextColor = ink.Ink });
        amount.Add(new Label { Text = share.Share, FontFamily = Fonts.Regular, FontSize = 14, TextColor = ink.Ink, VerticalOptions = LayoutOptions.Center });
        content.Add(name, 0, 0);
        content.Add(amount, 1, 0);
        var block = Surface(ink);
        block.Add(content);
        SemanticProperties.SetDescription(block, $"{share.Category.Name}, {share.Amount}, {share.Share}");
        // A block too low for a line of text keeps only its color.
        block.SizeChanged += (_, _) => content.IsVisible = block.Height >= 36;
        return block;
    }

    private View Half(RecapChange change, bool isUpper)
    {
        var ink = Ink(change.Surface);
        var who = new HorizontalStackLayout { Spacing = 10, Margin = new Thickness(0, 0, 0, 10) };
        who.Add(StoryBlocks.Tile(change.Category, 36));
        who.Add(new Label { Text = change.Category.Name, FontFamily = Fonts.Bold, FontSize = 18, TextColor = ink.Ink, VerticalOptions = LayoutOptions.Center });
        var content = Stack(
            who,
            StoryBlocks.Amount(change.Amount, ink, isTotal: false, margin: 4),
            Above(StoryBlocks.Phrase(new RecapPhrase(change.Detail), ink, 15), 8));
        var inner = new Grid
        {
            SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container, isUpper ? SafeAreaRegions.Container : SafeAreaRegions.None,
                SafeAreaRegions.Container, isUpper ? SafeAreaRegions.None : SafeAreaRegions.Container),
            Padding = isUpper ? new Thickness(Gutter, TopBar, Gutter, 26) : new Thickness(Gutter, 26, Gutter, 4),
            VerticalOptions = LayoutOptions.Fill
        };
        content.VerticalOptions = isUpper ? LayoutOptions.End : LayoutOptions.Start;
        inner.Add(content);
        var half = Surface(ink);
        half.Add(inner);
        return half;
    }

    private static Grid Surface(StoryInk ink) => new() { BackgroundColor = ink.Ground, SafeAreaEdges = SafeAreaEdges.None };

    /// <summary>A card on its surface: the top group, what is left, the bottom group.</summary>
    private static View Card(StoryInk ink, View? top, View? bottom)
    {
        var body = new Grid
        {
            SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container),
            Padding = new Thickness(Gutter, TopBar, Gutter, 4),
            RowDefinitions = [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto)]
        };
        if (top is not null)
            body.Add(top, 0, 0);

        if (bottom is not null)
            body.Add(bottom, 0, 2);

        var card = Surface(ink);
        card.Add(body);
        return card;
    }

    private static VerticalStackLayout Stack(params View[] children)
    {
        var stack = new VerticalStackLayout();
        foreach (var child in children)
            stack.Add(child);

        return stack;
    }

    private static View Above(View view, double space)
    {
        view.Margin = new Thickness(view.Margin.Left, view.Margin.Top + space, view.Margin.Right, view.Margin.Bottom);
        return view;
    }

    private static BoxView Space(double height) => new() { HeightRequest = height, Color = Colors.Transparent };

    private static Label Glyph(string glyph, StoryInk ink, double size, Color? color = null)
    {
        var label = new Label { Text = glyph, FontFamily = Fonts.Icons, FontSize = size, TextColor = color ?? ink.Mute, VerticalOptions = LayoutOptions.Center };
        AutomationProperties.SetIsInAccessibleTree(label, false);
        return label;
    }

    private static BoxView Dash(StoryInk ink) => new()
    {
        Color = ink.Ink,
        WidthRequest = 5,
        HeightRequest = 2,
        VerticalOptions = LayoutOptions.Center
    };
}
