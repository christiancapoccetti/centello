using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class RecapCardCategories_should_expected_behavior
{
    private const string Nbsp = " ";
    private static readonly string[] Colors = ["peri", "mint", "butter", "sky", "lilac", "coral", "mint", "peri", "sky"];

    private static async Task<RecapCard.Categories> November(params long[] cents)
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        for (var index = 0; index < cents.Length; index++)
            world.Spend(world.Category($"Categoria {index + 1}", Colors[index]), cents[index], new DateOnly(2026, 11, index + 1));

        for (var day = 12; day <= 28; day++)
            world.Spend(world.Categories.Items[0], 100, new DateOnly(2026, 11, day));

        return (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Categories>();
    }

    [Fact]
    public async Task Should_draw_a_block_per_category_as_tall_as_its_share()
    {
        var categories = await November(60_000, 30_000, 10_000);

        Assert.Equal(new RecapPhrase("Dove sono andati i ", $"1.017,00{Nbsp}€"), categories.Lead);
        var first = categories.Shares[0];
        Assert.Equal("Categoria 1", first.Category.Name);
        Assert.Equal($"617,00{Nbsp}€", first.Amount);
        Assert.Equal("61%", first.Share);
        Assert.Equal(61_700 / 101_700.0, first.Fraction);
        Assert.Equal(["Categoria 1", "Categoria 2", "Categoria 3"], categories.Shares.Select(share => share.Category.Name));
    }

    [Fact]
    public async Task Should_fold_the_categories_beyond_the_sixth_into_one_block()
    {
        var categories = await November(80_000, 70_000, 60_000, 50_000, 40_000, 30_000, 20_000, 10_000);

        Assert.Equal(7, categories.Shares.Count);
        var others = categories.Shares[^1];
        Assert.Equal("Altre 2 categorie", others.Category.Name);
        Assert.Equal($"300,00{Nbsp}€", others.Amount);
        Assert.Equal("grey", others.Category.ColorKey);
    }

    [Fact]
    public async Task Should_name_a_seventh_category_rather_than_fold_a_single_one()
    {
        var categories = await November(80_000, 70_000, 60_000, 50_000, 40_000, 30_000, 20_000);

        Assert.Equal(7, categories.Shares.Count);
        Assert.Equal("Categoria 7", categories.Shares[^1].Category.Name);
    }

    [Fact]
    public async Task Should_say_nothing_about_a_single_category()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        world.SpendInMonth(world.Category("Spesa", "mint"), 2026, 11, 100_000, 12);

        var deck = await world.Deck(Period.Month(2026, 11));

        Assert.DoesNotContain(deck.Cards, card => card is RecapCard.Categories);
    }

    [Fact]
    public async Task Should_keep_the_name_of_an_archived_category()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        world.SpendInMonth(world.Category("Spesa", "mint"), 2026, 11, 100_000, 12);
        world.SpendInMonth(world.Category("Vecchia", "sky", archived: true), 2026, 11, 50_000, 6);

        var categories = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Categories>();

        Assert.Equal(["Spesa", "Vecchia"], categories.Shares.Select(share => share.Category.Name));
    }

    [Fact]
    public async Task Should_end_on_the_block_of_the_last_category_even_when_it_is_coral()
    {
        var categories = await November(60_000, 30_000, 10_000, 5_000, 4_000, 3_000);

        Assert.Equal("coral", categories.Shares[^1].Category.ColorKey);
        Assert.Equal("coral", categories.Bottom.PaletteKey);
        Assert.True(categories.Top.IsGround);
    }

    [Fact]
    public async Task Should_leave_the_first_category_for_the_summary()
    {
        var world = new RecapWorld(new DateOnly(2026, 12, 1));
        world.SpendInMonth(world.Category("Casa", "peri"), 2026, 11, 75_000, 6);
        world.SpendInMonth(world.Category("Spesa", "mint"), 2026, 11, 25_000, 6);

        var categories = (await world.Deck(Period.Month(2026, 11))).Single<RecapCard.Categories>();

        Assert.Equal(new RecapSummaryRow(MaterialSymbols.DonutSmall, "Prima categoria", "Casa · 75%"), categories.Row);
    }

    [Fact]
    public async Task Should_round_a_year_to_whole_euros()
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        world.SpendInYear(world.Category("Casa", "peri"), 2026, 384_050, 20);
        world.SpendInYear(world.Category("Spesa", "mint"), 2026, 360_990, 20);

        var categories = (await world.Deck(Period.Year(2026))).Single<RecapCard.Categories>();

        Assert.Equal($"7.450{Nbsp}€", categories.Lead.Emphasis);
        Assert.Equal($"3.841{Nbsp}€", categories.Shares[0].Amount);
    }
}
