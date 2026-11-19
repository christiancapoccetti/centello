using Centesimo.Domain;

namespace Centesimo.Infrastructure.Tests;

public sealed class TagRepository_should_expected_behavior
{
    [Fact]
    public async Task Return_only_tags_for_requested_category()
    {
        using var database = new TestDatabase();
        var firstCategory = new Category(Guid.NewGuid(), "Food", "cart", "#123456");
        var secondCategory = new Category(Guid.NewGuid(), "Travel", "car", "#654321");
        database.Context.AddRange(firstCategory, secondCategory);
        await database.Context.SaveChangesAsync();
        var repository = new TagRepository(database.Context);
        await repository.Add(new Tag(Guid.NewGuid(), firstCategory.CategoryId, "Market"));
        await repository.Add(new Tag(Guid.NewGuid(), secondCategory.CategoryId, "Fuel"));
        database.Context.ChangeTracker.Clear();

        var tags = await repository.GetByCategory(firstCategory.CategoryId);

        Assert.Single(tags.Value);
        Assert.Equal("Market", tags.Value[0].Name);
        Assert.Empty(database.Context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task Return_the_tags_of_every_category_without_tracking_them()
    {
        using var database = new TestDatabase();
        var firstCategory = new Category(Guid.NewGuid(), "Food", "cart", "#123456");
        var secondCategory = new Category(Guid.NewGuid(), "Travel", "car", "#654321");
        database.Context.AddRange(firstCategory, secondCategory);
        await database.Context.SaveChangesAsync();
        var repository = new TagRepository(database.Context);
        await repository.Add(new Tag(Guid.NewGuid(), firstCategory.CategoryId, "Market"));
        await repository.Add(new Tag(Guid.NewGuid(), secondCategory.CategoryId, "Fuel"));
        database.Context.ChangeTracker.Clear();

        var tags = await repository.GetAll();

        Assert.Equal(["Fuel", "Market"], tags.Value.Select(tag => tag.Name));
        Assert.Empty(database.Context.ChangeTracker.Entries());
    }
}
