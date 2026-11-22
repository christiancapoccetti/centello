using Centello.Domain;
using Microsoft.EntityFrameworkCore;

namespace Centello.Infrastructure.Tests;

public sealed class CategoryRepository_should_expected_behavior
{
    [Fact]
    public async Task Add_and_read_without_tracking()
    {
        using var database = new TestDatabase();
        var repository = new CategoryRepository(database.Context);
        var category = new Category(Guid.NewGuid(), "Groceries", "cart", "#123456");

        await repository.Add(category, []);
        database.Context.ChangeTracker.Clear();
        var loaded = await repository.Get(category.CategoryId);

        Assert.NotNull(loaded);
        Assert.Empty(database.Context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Update_a_tracked_entity_loaded_by_repository()
    {
        using var database = new TestDatabase();
        var repository = new CategoryRepository(database.Context);
        var category = new Category(Guid.NewGuid(), "Groceries", "cart", "#123456");
        await repository.Add(category, []);
        database.Context.ChangeTracker.Clear();
        category.UpdateDetails("Dining", "home", "#176B5B", new Money(30_000));
        category.Archive();

        await repository.Update(category);
        database.Context.ChangeTracker.Clear();
        var stored = await database.Context.Categories.AsNoTracking().SingleAsync();

        Assert.Equal(30_000, stored.MonthlyBudget?.Cents);
        Assert.Equal("Dining", stored.Name);
        Assert.Equal("home", stored.Icon);
        Assert.True(stored.IsArchived);
    }

    [Fact]
    public async Task Restore_an_archived_category()
    {
        using var database = new TestDatabase();
        var repository = new CategoryRepository(database.Context);
        var category = new Category(Guid.NewGuid(), "Groceries", "cart", "#123456");
        category.Archive();
        await repository.Add(category, []);
        database.Context.ChangeTracker.Clear();
        category.Restore();

        await repository.Update(category);
        database.Context.ChangeTracker.Clear();
        var stored = await database.Context.Categories.AsNoTracking().SingleAsync();

        Assert.False(stored.IsArchived);
    }

    [Fact]
    public async Task Add_a_category_together_with_its_tags()
    {
        using var database = new TestDatabase();
        var repository = new CategoryRepository(database.Context);
        var category = new Category(Guid.NewGuid(), "Restaurants", "food", "mint");
        var tags = new[]
        {
            new Tag(Guid.NewGuid(), category.CategoryId, "Pizza"),
            new Tag(Guid.NewGuid(), category.CategoryId, "Takeaway")
        };

        var result = await repository.Add(category, tags);
        database.Context.ChangeTracker.Clear();

        Assert.True(result.IsSuccess);
        Assert.Equal(category.CategoryId, (await database.Context.Categories.AsNoTracking().SingleAsync()).CategoryId);
        Assert.Equal(["Pizza", "Takeaway"],
            database.Context.Tags.AsNoTracking().OrderBy(tag => tag.Name).Select(tag => tag.Name));
    }

    [Fact]
    public async Task Add_neither_the_category_nor_its_tags_when_a_tag_cannot_be_stored()
    {
        using var database = new TestDatabase();
        var repository = new CategoryRepository(database.Context);
        var category = new Category(Guid.NewGuid(), "Restaurants", "food", "mint");
        var tags = new[]
        {
            new Tag(Guid.NewGuid(), category.CategoryId, "Pizza"),
            new Tag(Guid.NewGuid(), category.CategoryId, "Pizza")
        };

        var result = await repository.Add(category, tags);
        database.Context.ChangeTracker.Clear();

        Assert.True(result.IsFailure);
        Assert.Empty(database.Context.Categories.AsNoTracking());
        Assert.Empty(database.Context.Tags.AsNoTracking());
    }
}
