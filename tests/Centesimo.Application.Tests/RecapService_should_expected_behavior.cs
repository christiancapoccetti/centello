using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapService_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 21);

    private static RecapWorld WorldWithMonth(int year, int month, int count = 12, DateOnly? today = null)
    {
        var world = new RecapWorld(today ?? Today);
        world.SpendInMonth(world.Category("Spesa", "mint"), year, month, 100_000, count);
        return world;
    }

    private static RecapWorld WorldWithYear(int year, int count = 40, int lastMonth = 12, DateOnly? today = null)
    {
        var world = new RecapWorld(today ?? Today);
        world.SpendInYear(world.Category("Spesa", "mint"), year, 1_200_000, count, lastMonth);
        return world;
    }

    private static RecapWorld WorldWithMonthOnFewDays()
    {
        var world = new RecapWorld(Today);
        var spesa = world.Category("Spesa", "mint");
        for (var index = 0; index < 12; index++)
            world.Spend(spesa, 5_000, new DateOnly(2026, 10, index % 4 + 1));

        return world;
    }

    private static readonly Dictionary<string, Func<(RecapWorld World, Period Period)>> UnavailableCases = new()
    {
        ["the month of today"] = () => (WorldWithMonth(2026, 11), Period.Month(2026, 11)),
        ["a future month"] = () => (WorldWithMonth(2026, 12), Period.Month(2026, 12)),
        ["a thin month"] = () => (WorldWithMonth(2026, 10, count: 9), Period.Month(2026, 10)),
        ["a month on too few days"] = () => (WorldWithMonthOnFewDays(), Period.Month(2026, 10)),
        ["a future year"] = () => (WorldWithYear(2027), Period.Year(2027)),
        ["a thin year"] = () => (WorldWithYear(2025, count: 29), Period.Year(2025)),
        ["a year in two months"] = () => (WorldWithYear(2025, lastMonth: 2), Period.Year(2025)),
        ["1 January for the current year"] = () =>
            (WorldWithYear(2025, today: new DateOnly(2026, 1, 1)), Period.Year(2026))
    };

    private static readonly Dictionary<string, Func<(RecapWorld World, Period Period)>> AvailableCases = new()
    {
        ["a closed month"] = () => (WorldWithMonth(2026, 10), Period.Month(2026, 10)),
        ["a month of 10 expenses"] = () => (WorldWithMonth(2026, 10, count: 10), Period.Month(2026, 10)),
        ["a closed year"] = () => (WorldWithYear(2025), Period.Year(2025)),
        ["the year so far"] = () => (WorldWithYear(2026, lastMonth: 10), Period.Year(2026))
    };

    public static IEnumerable<object[]> Unavailable => UnavailableCases.Keys.Select(name => new object[] { name });

    public static IEnumerable<object[]> Available => AvailableCases.Keys.Select(name => new object[] { name });

    [Theory]
    [MemberData(nameof(Unavailable))]
    public async Task Should_not_build_a_recap_for(string situation)
    {
        var (world, period) = UnavailableCases[situation]();

        var result = await world.Service.Get(period);

        Assert.True(result.IsFailure, situation);
        Assert.Equal(RecapErrors.NotAvailable, result.Error);
        Assert.False((await world.Service.IsAvailable(period)).Value, situation);
    }

    [Theory]
    [MemberData(nameof(Available))]
    public async Task Should_build_a_recap_for(string situation)
    {
        var (world, period) = AvailableCases[situation]();

        var result = await world.Service.Get(period);

        Assert.True(result.IsSuccess, situation);
        Assert.Equal(period, result.Value.Span.Period);
        Assert.True((await world.Service.IsAvailable(period)).Value, situation);
    }

    [Fact]
    public async Task Should_open_with_the_cover_and_close_with_the_summary()
    {
        var deck = await WorldWithMonth(2026, 10).Deck(Period.Month(2026, 10));

        Assert.IsType<RecapCard.Cover>(deck.Cards[0]);
        Assert.IsType<RecapCard.Summary>(deck.Cards[^1]);
        Assert.IsType<RecapCard.Total>(deck.Cards[1]);
    }

    [Fact]
    public async Task Should_derive_the_summary_rows_from_the_cards_between_the_cover_and_the_summary()
    {
        var deck = await WorldWithYear(2025).Deck(Period.Year(2025));

        var summary = deck.Single<RecapCard.Summary>();
        var expected = deck.Cards.Skip(1).SkipLast(1).Select(card => card.Row).OfType<RecapSummaryRow>();
        Assert.Equal(expected, summary.Rows);
        Assert.NotEmpty(summary.Rows);
    }

    [Fact]
    public async Task Should_await_the_reads_one_at_a_time()
    {
        var world = WorldWithMonth(2026, 10);
        var gate = new Gate();
        var service = new RecapService(
            new GatedCategories(world.Categories, gate),
            new GatedTags(world.Tags, gate),
            new GatedExpenses(world.Expenses, gate),
            () => Today);

        var result = await service.Get(Period.Month(2026, 10));

        Assert.True(result.IsSuccess);
        Assert.Equal(3, gate.Reads);
        Assert.False(gate.Overlapped);
    }

    [Fact]
    public async Task Should_return_a_category_failure_unchanged()
    {
        var world = WorldWithMonth(2026, 10);
        var failure = new Error("Test.Categories", "categories");
        world.Categories.Failure = failure;

        Assert.Equal(failure, (await world.Service.Get(Period.Month(2026, 10))).Error);
    }

    [Fact]
    public async Task Should_return_a_tag_failure_unchanged()
    {
        var world = WorldWithMonth(2026, 10);
        var failure = new Error("Test.Tags", "tags");
        world.Tags.Failure = failure;

        Assert.Equal(failure, (await world.Service.Get(Period.Month(2026, 10))).Error);
    }

    [Fact]
    public async Task Should_return_an_expense_failure_unchanged_from_both_methods()
    {
        var world = WorldWithMonth(2026, 10);
        var failure = new Error("Test.Expenses", "expenses");
        world.Expenses.Failure = failure;

        Assert.Equal(failure, (await world.Service.Get(Period.Month(2026, 10))).Error);
        Assert.Equal(failure, (await world.Service.IsAvailable(Period.Month(2026, 10))).Error);
    }

    [Fact]
    public async Task Should_read_only_the_period_to_know_whether_it_is_available()
    {
        var world = WorldWithMonth(2026, 10);
        world.Categories.Failure = new Error("Test.Categories", "categories");
        world.Tags.Failure = new Error("Test.Tags", "tags");

        Assert.True((await world.Service.IsAvailable(Period.Month(2026, 10))).Value);
    }

    [Fact]
    public async Task Should_count_the_expenses_of_the_span_and_not_those_of_the_compared_year()
    {
        var world = WorldWithMonth(2026, 10, count: 9);
        world.SpendInMonth(world.Categories.Items[0], 2025, 10, 100_000, 30);

        Assert.False((await world.Service.IsAvailable(Period.Month(2026, 10))).Value);
        Assert.True((await world.Service.Get(Period.Month(2026, 10))).IsFailure);
    }

    private sealed class Gate
    {
        private int _active;
        public int Reads { get; private set; }
        public bool Overlapped { get; private set; }

        public async Task<T> Read<T>(Func<Task<T>> read)
        {
            Reads++;
            if (++_active > 1)
                Overlapped = true;

            await Task.Delay(15);
            var result = await read();
            _active--;
            return result;
        }
    }

    private sealed class GatedCategories(FakeCategoryRepository inner, Gate gate) : ICategoryRepository
    {
        public Task<Result> Add(Category category, IReadOnlyList<Tag> tags, CancellationToken cancellationToken = default) =>
            inner.Add(category, tags, cancellationToken);

        public Task<Result<Category?>> Get(Guid categoryId, CancellationToken cancellationToken = default) =>
            inner.Get(categoryId, cancellationToken);

        public Task<Result<IReadOnlyList<Category>>> GetAll(CancellationToken cancellationToken = default) =>
            gate.Read(() => inner.GetAll(cancellationToken));

        public Task<Result> Update(Category category, CancellationToken cancellationToken = default) =>
            inner.Update(category, cancellationToken);
    }

    private sealed class GatedTags(FakeTagRepository inner, Gate gate) : ITagRepository
    {
        public Task<Result> Add(Tag tag, CancellationToken cancellationToken = default) =>
            inner.Add(tag, cancellationToken);

        public Task<Result<Tag?>> Get(Guid tagId, CancellationToken cancellationToken = default) =>
            inner.Get(tagId, cancellationToken);

        public Task<Result<IReadOnlyList<Tag>>> GetAll(CancellationToken cancellationToken = default) =>
            gate.Read(() => inner.GetAll(cancellationToken));

        public Task<Result<IReadOnlyList<Tag>>> GetByCategory(Guid categoryId,
            CancellationToken cancellationToken = default) => inner.GetByCategory(categoryId, cancellationToken);

        public Task<Result> Update(Tag tag, CancellationToken cancellationToken = default) =>
            inner.Update(tag, cancellationToken);
    }

    private sealed class GatedExpenses(FakeExpenseRepository inner, Gate gate) : IExpenseRepository
    {
        public Task<Result> Add(Expense expense, CancellationToken cancellationToken = default) =>
            inner.Add(expense, cancellationToken);

        public Task<Result<Expense?>> Get(Guid expenseId, CancellationToken cancellationToken = default) =>
            inner.Get(expenseId, cancellationToken);

        public Task<Result<IReadOnlyList<Expense>>> GetByCategoryBetween(Guid categoryId, DateOnly from, DateOnly to,
            CancellationToken cancellationToken = default) =>
            inner.GetByCategoryBetween(categoryId, from, to, cancellationToken);

        public Task<Result<IReadOnlyList<Expense>>> GetBetween(DateOnly from, DateOnly to,
            CancellationToken cancellationToken = default) => gate.Read(() => inner.GetBetween(from, to, cancellationToken));

        public Task<Result> Update(Expense expense, CancellationToken cancellationToken = default) =>
            inner.Update(expense, cancellationToken);

        public Task<Result> Delete(Guid expenseId, CancellationToken cancellationToken = default) =>
            inner.Delete(expenseId, cancellationToken);
    }
}
