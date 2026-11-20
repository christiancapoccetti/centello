using System.Globalization;
using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class WaterfallChart_should_expected_behavior
{
    private const char Minus = '\u2212';
    private static readonly DateOnly Today = new(2026, 11, 19);

    private enum Standing { Active, Archived, Unknown }

    private sealed record Line(string Name, long Previous, long Current, Standing Standing = Standing.Active,
        string Icon = "cart", string Color = "coral");

    [Fact]
    public async Task Should_leave_out_the_waterfall_without_spending_in_the_compared_days()
    {
        var fixtures = InsightFixtures.November();
        fixtures.Expenses.Items.RemoveAll(expense => expense.OccurredOn <= new DateOnly(2026, 10, 19));

        var late = await fixtures.Overview();
        var none = await Overview([]);

        Assert.Null(late.Waterfall);
        Assert.False(late.HasWaterfall);
        Assert.Null(none.Waterfall);
        Assert.False(none.HasWaterfall);
    }

    [Fact]
    public async Task Should_leave_out_the_waterfall_when_nothing_changed()
    {
        var overview = await Overview([new Line("Spesa", 10_000, 10_000), new Line("Casa", 5_000, 5_000)]);

        Assert.Null(overview.Waterfall);
        Assert.False(overview.HasWaterfall);
    }

    [Fact]
    public async Task Should_run_from_the_compared_total_to_the_current_total()
    {
        var overview = await InsightFixtures.November().Overview();

        var chart = overview.Waterfall!;

        Assert.True(overview.HasWaterfall);
        Assert.Equal(InsightFixtures.ComparedCents, chart.Start.Cents);
        Assert.Equal(InsightFixtures.SpentCents, chart.End.Cents);
        Assert.Equal(overview.ComparedSpentCents, chart.Start.Cents);
        Assert.Equal(overview.SpentCents, chart.End.Cents);
    }

    [Fact]
    public async Task Should_reconcile_to_the_cent()
    {
        var chart = await Chart(MessyMonth());

        Assert.Equal(78_000, chart.Start.Cents);
        Assert.Equal(67_044, chart.End.Cents);
        Assert.Equal(["A", "B", "I", "Altre categorie", "C", "D"], chart.Steps.Select(step => step.Name));
        Assert.Equal([-8_000, -4_000, -2_000, -1_456, 1_500, 3_000], chart.Steps.Select(step => step.DeltaCents));
        Assert.Equal(chart.End.Cents, chart.Start.Cents + chart.Steps.Sum(step => step.DeltaCents));
    }

    [Fact]
    public async Task Should_order_steps_falls_first()
    {
        var chart = (await InsightFixtures.November().Overview()).Waterfall!;

        Assert.Equal(["Trasporti", "Svago", "Salute", "Altro", "Spesa", "Ristoranti"], chart.Steps.Select(step => step.Name));
        Assert.Equal([-5_940, -4_620, -2_680, 440, 1_380, 3_220], chart.Steps.Select(step => step.DeltaCents));
        Assert.Equal([false, false, false, true, true, true], chart.Steps.Select(step => step.IsRise));
    }

    [Fact]
    public async Task Should_order_steps_of_the_same_change_by_name()
    {
        var chart = await Chart(new Line("Zeta", 20_000, 10_000), new Line("Alfa", 20_000, 10_000), new Line("Beta", 5_000, 8_000));

        Assert.Equal(["Alfa", "Zeta", "Beta"], chart.Steps.Select(step => step.Name));
    }

    [Fact]
    public async Task Should_step_a_category_that_dropped_to_zero()
    {
        var overview = await Overview([new Line("Palestra", 30_000, 0), new Line("Spesa", 10_000, 20_000)]);

        var chart = overview.Waterfall!;

        Assert.Equal(["Spesa"], overview.Categories.Select(category => category.Name));
        Assert.Equal(["Palestra", "Spesa"], chart.Steps.Select(step => step.Name));
        Assert.Equal([-30_000, 10_000], chart.Steps.Select(step => step.DeltaCents));
        Assert.Equal(40_000, chart.Start.Cents);
        Assert.Equal(20_000, chart.End.Cents);
    }

    [Fact]
    public async Task Should_leave_out_unchanged_categories()
    {
        var chart = (await InsightFixtures.November().Overview()).Waterfall!;

        Assert.DoesNotContain(chart.Steps, step => step.Name == "Casa");
        Assert.Equal(6, chart.Steps.Count);
    }

    [Fact]
    public async Task Should_fold_archived_and_unknown_categories_into_altre_categorie()
    {
        var chart = await Chart(
            new Line("Spesa", 10_000, 6_000),
            new Line("Archiviata", 5_000, 2_000, Standing.Archived),
            new Line("Sconosciuta", 0, 1_000, Standing.Unknown));

        var fold = Assert.Single(chart.Steps, step => step.Name == "Altre categorie");

        Assert.Equal(2, chart.Steps.Count);
        Assert.Equal(-2_000, fold.DeltaCents);
        Assert.Equal(("more", "grey"), (fold.IconKey, fold.ColorKey));
        Assert.Equal(15_000, chart.Start.Cents);
        Assert.Equal(9_000, chart.End.Cents);
    }

    [Fact]
    public async Task Should_fold_the_smallest_changes_past_six_steps()
    {
        var chart = await Chart(
            new Line("A", 100_000, 19_000),
            new Line("B", 100_000, 30_000),
            new Line("C", 100_000, 41_000),
            new Line("D", 100_000, 50_000),
            new Line("E", 100_000, 60_000),
            new Line("F", 100_000, 69_000),
            new Line("G", 100_000, 80_000),
            new Line("H", 100_000, 90_000));

        Assert.Equal(["A", "B", "Altre categorie", "C", "D", "E"], chart.Steps.Select(step => step.Name));
        Assert.Equal([-81_000, -70_000, -61_000, -59_000, -50_000, -40_000], chart.Steps.Select(step => step.DeltaCents));
        Assert.Equal(chart.End.Cents, chart.Start.Cents + chart.Steps.Sum(step => step.DeltaCents));
    }

    [Fact]
    public async Task Should_fold_changes_under_one_euro()
    {
        var chart = await Chart(new Line("A", 10_000, 6_000), new Line("B", 5_000, 5_099), new Line("C", 3_000, 3_100));

        Assert.Equal(["A", "Altre categorie", "C"], chart.Steps.Select(step => step.Name));
        Assert.Equal([-4_000, 99, 100], chart.Steps.Select(step => step.DeltaCents));
        Assert.Equal(chart.End.Cents, chart.Start.Cents + chart.Steps.Sum(step => step.DeltaCents));
    }

    [Fact]
    public async Task Should_chain_each_step_from_where_the_last_one_ended()
    {
        var chart = (await InsightFixtures.November().Overview()).Waterfall!;

        Assert.Equal(chart.Start.Level, chart.Steps[0].From);
        for (var index = 0; index < chart.Steps.Count - 1; index++)
            Assert.Equal(chart.Steps[index].To, chart.Steps[index + 1].From);

        Assert.Equal(chart.End.Level, chart.Steps[^1].To);
    }

    [Fact]
    public async Task Should_carry_each_category_icon_and_colour()
    {
        var mockup = (await InsightFixtures.November().Overview()).Waterfall!;
        var legacy = await Chart(new Line("Vecchia", 10_000, 4_000, Icon: "gioco", Color: "#196D61"));

        Assert.Equal(
            [("bus", "butter"), ("leisure", "lilac"), ("health", "sky"), ("more", "grey"), ("cart", "coral"), ("food", "mint")],
            mockup.Steps.Select(step => (step.IconKey, step.ColorKey)));
        Assert.Equal(("more", "grey"), (legacy.Steps[0].IconKey, legacy.Steps[0].ColorKey));
    }

    [Fact]
    public async Task Should_title_and_caption_months_and_years()
    {
        var lines = new[] { new Line("Spesa", 20_000, 10_000) };

        var november = (await Overview(lines)).Waterfall!;
        var april = (await Overview(lines, Period.Month(2026, 4))).Waterfall!;
        var year = (await Overview(lines, Period.Year(2026))).Waterfall!;

        Assert.Equal(("Da ottobre a novembre", "ott", "nov"), (november.Title, november.Start.Caption, november.End.Caption));
        Assert.Equal(("Da marzo ad aprile", "mar", "apr"), (april.Title, april.Start.Caption, april.End.Caption));
        Assert.Equal(("Dal 2025 al 2026", "2025", "2026"), (year.Title, year.Start.Caption, year.End.Caption));
    }

    // The running levels 136.660, 130.720, 126.100, 123.420, 123.860, 125.240 and 128.460 cents are 1.367, 1.307,
    // 1.261, 1.234, 1.239, 1.252 and 1.285 euros, and each label is the difference of two neighbours.
    [Fact]
    public async Task Should_round_labels_so_they_add_up()
    {
        var mockup = (await InsightFixtures.November().Overview()).Waterfall!;

        Assert.Equal("1.367", mockup.Start.Value);
        Assert.Equal([$"{Minus}60", $"{Minus}46", $"{Minus}27", "+5", "+13", "+33"], mockup.Steps.Select(step => step.Value));
        Assert.Equal("1.285", mockup.End.Value);
    }

    // Rounding each +1,40 € on its own gives +1 three times, 3 euros between 10 and 15.
    [Fact]
    public async Task Should_round_labels_so_they_add_up_where_rounding_each_step_would_not()
    {
        var chart = await Chart(new Line("A", 300, 440), new Line("B", 300, 440), new Line("C", 440, 580));

        Assert.Equal("10", chart.Start.Value);
        Assert.Equal(["+2", "+1", "+2"], chart.Steps.Select(step => step.Value));
        Assert.Equal("15", chart.End.Value);
    }

    [Fact]
    public async Task Should_add_the_labels_up_across_a_fold()
    {
        var chart = await Chart(MessyMonth());

        Assert.Equal("780", chart.Start.Value);
        Assert.Equal([$"{Minus}80", $"{Minus}40", $"{Minus}20", $"{Minus}15", "+15", "+30"], chart.Steps.Select(step => step.Value));
        Assert.Equal("670", chart.End.Value);
        Assert.Equal(Whole(chart.End.Value), Whole(chart.Start.Value) + chart.Steps.Sum(step => Whole(step.Value)));
    }

    [Theory]
    [InlineData(1_000, 1_100)]
    [InlineData(1_049, 1_149)]
    [InlineData(1_050, 1_150)]
    [InlineData(1_099, 1_199)]
    [InlineData(1_100, 1_000)]
    [InlineData(1_149, 1_049)]
    [InlineData(1_150, 1_050)]
    [InlineData(1_199, 1_099)]
    public async Task Should_never_label_a_change_of_one_euro_or_more_as_zero(long previous, long current)
    {
        var chart = await Chart(new Line("Spesa", previous, current));

        Assert.Equal(current > previous ? "+1" : $"{Minus}1", chart.Steps.Single().Value);
    }

    // Seven changes of at least 1 €, two under it, one category that dropped to zero, one unchanged, and spending in an
    // archived and in an unknown category: 780,00 € in October, 670,44 € in November.
    private static Line[] MessyMonth() =>
    [
        new("A", 20_000, 12_000),
        new("B", 15_000, 11_000),
        new("C", 9_000, 10_500),
        new("D", 5_000, 8_000),
        new("E", 7_000, 7_050),
        new("F", 6_000, 5_960),
        new("G", 4_000, 4_900),
        new("H", 3_000, 3_000),
        new("I", 2_000, 0),
        new("J", 1_000, 1_400),
        new("Archiviata", 6_000, 2_000, Standing.Archived),
        new("Sconosciuta", 0, 1_234, Standing.Unknown)
    ];

    private static long Whole(string label) =>
        long.Parse(label.Replace(".", "").Replace(Minus, '-'), CultureInfo.InvariantCulture);

    private static async Task<WaterfallChart> Chart(params Line[] lines) => (await Overview(lines)).Waterfall!;

    private static async Task<InsightsOverview> Overview(IEnumerable<Line> lines, Period? period = null)
    {
        var selected = period ?? Period.Month(2026, 11);
        var categories = new FakeCategoryRepository();
        var expenses = new FakeExpenseRepository();
        foreach (var line in lines)
        {
            var categoryId = Guid.NewGuid();
            if (line.Standing != Standing.Unknown)
                categories.Items.Add(CategoryOf(line, categoryId));

            Spend(expenses, categoryId, line.Previous, selected.Previous().Start.AddDays(4));
            Spend(expenses, categoryId, line.Current, selected.Start.AddDays(4));
        }

        var service = new InsightsService(categories, expenses, () => Today);
        return (await service.Get(selected)).Value;
    }

    private static Category CategoryOf(Line line, Guid categoryId)
    {
        var category = new Category(categoryId, line.Name, line.Icon, line.Color);
        if (line.Standing == Standing.Archived)
            category.Archive();

        return category;
    }

    private static void Spend(FakeExpenseRepository expenses, Guid categoryId, long cents, DateOnly on)
    {
        if (cents > 0)
            expenses.Items.Add(new Expense(Guid.NewGuid(), categoryId, new Money(cents), on));
    }
}
