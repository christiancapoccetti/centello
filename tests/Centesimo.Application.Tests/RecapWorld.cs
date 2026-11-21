using Centesimo.Domain;

namespace Centesimo.Application.Tests;

/// <summary>A small world of categories, tags and expenses behind the repository fakes, and the deck the PUBLIC
/// <see cref="RecapService"/> builds from it on a fixed day. Each instance is its own world.</summary>
internal sealed partial class RecapWorld(DateOnly? today = null)
{
    public static readonly DateOnly Today = new(2026, 12, 1);

    private readonly DateOnly _today = today ?? Today;

    public FakeCategoryRepository Categories { get; } = new();
    public FakeTagRepository Tags { get; } = new();
    public FakeExpenseRepository Expenses { get; } = new();

    public RecapService Service => new(Categories, Tags, Expenses, () => _today);

    public Category Category(string name, string colorKey, bool archived = false)
    {
        var category = new Category(Guid.NewGuid(), name, "more", colorKey);
        if (archived)
            category.Archive();

        Categories.Items.Add(category);
        return category;
    }

    public Tag Tag(Category category, string name)
    {
        var tag = new Tag(Guid.NewGuid(), category.CategoryId, name);
        Tags.Items.Add(tag);
        return tag;
    }

    public Expense Spend(Category category, long cents, DateOnly on, Tag? tag = null, string note = "",
        bool automatic = false, Guid? payment = null)
    {
        var expense = new Expense(Guid.NewGuid(), category.CategoryId, new Money(cents), on, tag?.TagId, note,
            recurringPaymentId: payment ?? (automatic ? Guid.NewGuid() : null));
        Expenses.Items.Add(expense);
        return expense;
    }

    public void SpendOnDays(Category category, long cents, params DateOnly[] days)
    {
        foreach (var day in days)
            Spend(category, cents, day);
    }

    /// <summary><paramref name="count"/> expenses on the first days of the month (wrapping after the 28th) that add up
    /// to <paramref name="totalCents"/>: the first one takes the remainder.</summary>
    public void SpendInMonth(Category category, int year, int month, long totalCents, int count)
    {
        var each = totalCents / count;
        for (var index = 0; index < count; index++)
            Spend(category, index == 0 ? each + totalCents % count : each,
                new DateOnly(year, month, index % 28 + 1));
    }

    /// <summary><paramref name="count"/> expenses spread over the months of a year (up to and including
    /// <paramref name="lastMonth"/>) that add up to <paramref name="totalCents"/>.</summary>
    public void SpendInYear(Category category, int year, long totalCents, int count, int lastMonth = 12)
    {
        var each = totalCents / count;
        for (var index = 0; index < count; index++)
            Spend(category, index == 0 ? each + totalCents % count : each,
                new DateOnly(year, index % lastMonth + 1, index / lastMonth % 28 + 1));
    }

    /// <summary>One expense of <paramref name="cents"/> carrying <paramref name="tag"/> per use, <paramref name="perMonth"/>
    /// (January first) of them in each month of the year.</summary>
    public void SpendWithTag(Category category, Tag tag, long cents, int year, params int[] perMonth)
    {
        for (var month = 1; month <= perMonth.Length; month++)
            for (var use = 0; use < perMonth[month - 1]; use++)
                Spend(category, cents, new DateOnly(year, month, use % 28 + 1), tag);
    }

    public async Task<RecapDeck> Deck(Period period) => (await Service.Get(period)).Value;
}

internal static class RecapDeckExtensions
{
    public static T Single<T>(this RecapDeck deck) where T : RecapCard => Assert.Single(deck.Cards.OfType<T>());
}
