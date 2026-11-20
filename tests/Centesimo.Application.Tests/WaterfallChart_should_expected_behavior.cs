using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class WaterfallChart_should_expected_behavior
{
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
        var chart = await Chart(
            new Line("A", 20_000, 12_000),
            new Line("B", 15_000, 11_000),
            new Line("C", 9_000, 10_500),
            new Line("D", 5_000, 8_000),
            new Line("E", 7_000, 7_050),
            new Line("F", 6_000, 5_960),
            new Line("G", 4_000, 4_900),
            new Line("H", 3_000, 3_000),
            new Line("I", 2_000, 0),
            new Line("J", 1_000, 1_400),
            new Line("Archiviata", 6_000, 2_000, Standing.Archived),
            new Line("Sconosciuta", 0, 1_234, Standing.Unknown));

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
