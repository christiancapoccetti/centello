using Centello.Domain;

namespace Centello.Infrastructure.Tests;

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

    [Fact]
    public async Task Store_a_tag_archived_and_then_restored()
    {
        using var database = new TestDatabase();
        var category = new Category(Guid.NewGuid(), "Food", "cart", "#123456");
        database.Context.Add(category);
        await database.Context.SaveChangesAsync();
        var repository = new TagRepository(database.Context);
        var tagId = Guid.NewGuid();
        await repository.Add(new Tag(tagId, category.CategoryId, "Market"));
        database.Context.ChangeTracker.Clear();

        var loaded = (await repository.Get(tagId)).Value!;
        loaded.Archive();
        var archived = await repository.Update(loaded);
        database.Context.ChangeTracker.Clear();
        var afterArchive = (await repository.Get(tagId)).Value!;
        afterArchive.Restore();
        var restored = await repository.Update(afterArchive);
        database.Context.ChangeTracker.Clear();

        Assert.True(archived.IsSuccess);
        Assert.True(restored.IsSuccess);
        Assert.False((await repository.Get(tagId)).Value!.IsArchived);
    }

    [Fact]
    public async Task Store_an_archived_tag()
    {
        using var database = new TestDatabase();
        var category = new Category(Guid.NewGuid(), "Food", "cart", "#123456");
        database.Context.Add(category);
        await database.Context.SaveChangesAsync();
        var repository = new TagRepository(database.Context);
        var tagId = Guid.NewGuid();
        await repository.Add(new Tag(tagId, category.CategoryId, "Market"));
        database.Context.ChangeTracker.Clear();

        var loaded = (await repository.Get(tagId)).Value!;
        loaded.Archive();
        await repository.Update(loaded);
        database.Context.ChangeTracker.Clear();

        Assert.True((await repository.Get(tagId)).Value!.IsArchived);
    }
}
