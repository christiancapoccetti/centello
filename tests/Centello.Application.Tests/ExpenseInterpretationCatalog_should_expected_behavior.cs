namespace Centello.Application.Tests;

public sealed class ExpenseInterpretationCatalog_should_expected_behavior
{
    private static readonly DraftCategory Car = new(
        Guid.NewGuid(),
        "Auto",
        [new DraftTag(Guid.NewGuid(), "Carburante"), new DraftTag(Guid.NewGuid(), "Tagliando")]);

    private static readonly DraftCategory Home = new(
        Guid.NewGuid(),
        "Casa",
        [new DraftTag(Guid.NewGuid(), "Altro")]);

    [Fact]
    public void Assign_category_and_tag_keys_in_input_order()
    {
        var catalog = new ExpenseInterpretationCatalog([Car, Home]);

        Assert.Equal(["c1", "c2"], catalog.Categories.Select(category => category.Key));
        Assert.Equal(["t1", "t2", "t3"], catalog.Categories.SelectMany(category => category.Tags).Select(tag => tag.Key));
    }

    [Fact]
    public void Keep_tag_keys_unique_across_categories_with_the_same_tag_name()
    {
        var first = new DraftCategory(Guid.NewGuid(), "Auto", [new DraftTag(Guid.NewGuid(), "Altro")]);
        var second = new DraftCategory(Guid.NewGuid(), "Casa", [new DraftTag(Guid.NewGuid(), "Altro")]);
        var catalog = new ExpenseInterpretationCatalog([first, second]);

        Assert.Equal(["t1", "t2"], catalog.Categories.SelectMany(category => category.Tags).Select(tag => tag.Key));
        Assert.Equal(first.Tags.Single().TagId, catalog.FindCategory("c1")!.FindTag("t1")!.Tag.TagId);
        Assert.Equal(second.Tags.Single().TagId, catalog.FindCategory("c2")!.FindTag("t2")!.Tag.TagId);
    }

    [Fact]
    public void Find_a_category_by_key()
    {
        var catalog = new ExpenseInterpretationCatalog([Car, Home]);

        Assert.Same(Home, catalog.FindCategory("c2")!.Category);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("c9")]
    [InlineData("C1")]
    [InlineData(" c1")]
    public void Return_nothing_for_an_unknown_category_key(string? key)
    {
        var catalog = new ExpenseInterpretationCatalog([Car, Home]);

        Assert.Null(catalog.FindCategory(key));
    }

    [Fact]
    public void Find_a_tag_only_inside_its_own_category()
    {
        var catalog = new ExpenseInterpretationCatalog([Car, Home]);
        var car = catalog.FindCategory("c1")!;

        Assert.Equal("Tagliando", car.FindTag("t2")!.Tag.Name);
        Assert.Null(car.FindTag("t3"));
        Assert.Null(car.FindTag(null));
    }

    [Fact]
    public void Render_categories_with_their_indented_tags()
    {
        var catalog = new ExpenseInterpretationCatalog([Car, Home]);

        Assert.Equal(
            [
                "c1: Auto",
                "  t1: Carburante",
                "  t2: Tagliando",
                "c2: Casa",
                "  t3: Altro"
            ],
            catalog.PromptLines);
    }

    [Fact]
    public void Be_empty_without_categories()
    {
        var catalog = new ExpenseInterpretationCatalog([]);

        Assert.Empty(catalog.Categories.Select(category => category.Key));
        Assert.Empty(catalog.Categories.SelectMany(category => category.Tags).Select(tag => tag.Key));
        Assert.Empty(catalog.PromptLines);
        Assert.Null(catalog.FindCategory("c1"));
    }
}
