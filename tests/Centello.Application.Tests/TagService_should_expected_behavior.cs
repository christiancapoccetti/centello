using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class TagService_should_expected_behavior
{
    [Fact]
    public async Task Should_return_only_active_tags_ordered_by_name()
    {
        var categoryId = Guid.NewGuid();
        var categories = new FakeCategoryRepository();
        var tags = new FakeTagRepository();
        var archived = new Tag(Guid.NewGuid(), categoryId, "Archived");
        archived.Archive();
        tags.Items.AddRange([
            new Tag(Guid.NewGuid(), categoryId, "Work"),
            archived,
            new Tag(Guid.NewGuid(), categoryId, "Family")
        ]);
        var service = new TagService(categories, tags);

        var result = await service.GetActive(categoryId);

        Assert.True(result.IsSuccess);
        Assert.Collection(
            result.Value,
            tag => Assert.Equal("Family", tag.Name),
            tag => Assert.Equal("Work", tag.Name));
    }

    [Fact]
    public async Task Should_create_and_archive_a_tag()
    {
        var category = new Category(Guid.NewGuid(), "Food", "cart", "#176B5B");
        var categories = new FakeCategoryRepository();
        categories.Items.Add(category);
        var tags = new FakeTagRepository();
        var service = new TagService(categories, tags);

        var created = await service.Create(category.CategoryId, "Lunch");
        var archived = await service.Archive(created.Value.TagId);

        Assert.True(created.IsSuccess);
        Assert.True(archived.IsSuccess);
        Assert.True(created.Value.IsArchived);
        Assert.Empty((await service.GetActive(category.CategoryId)).Value);
    }

    [Fact]
    public async Task Should_return_only_archived_tags_of_the_category_ordered_by_name()
    {
        var categoryId = Guid.NewGuid();
        var tags = new FakeTagRepository();
        tags.Items.AddRange([
            ArchivedTag(categoryId, "Pranzo"),
            new Tag(Guid.NewGuid(), categoryId, "Cena"),
            ArchivedTag(categoryId, "Domicilio"),
            ArchivedTag(Guid.NewGuid(), "Altrove")
        ]);
        var service = new TagService(new FakeCategoryRepository(), tags);

        var result = await service.GetArchived(categoryId);

        Assert.Equal(["Domicilio", "Pranzo"], result.Value.Select(tag => tag.Name));
    }

    [Fact]
    public async Task Should_restore_an_archived_tag()
    {
        var categoryId = Guid.NewGuid();
        var tags = new FakeTagRepository();
        var tag = ArchivedTag(categoryId, "Domicilio");
        tags.Items.Add(tag);
        var service = new TagService(new FakeCategoryRepository(), tags);

        var restored = await service.Restore(tag.TagId);

        Assert.True(restored.IsSuccess);
        Assert.Equal(["Domicilio"], (await service.GetActive(categoryId)).Value.Select(item => item.Name));
        Assert.Empty((await service.GetArchived(categoryId)).Value);
    }

    [Fact]
    public async Task Should_say_the_tag_is_missing_when_archiving_or_restoring_an_unknown_one()
    {
        var service = new TagService(new FakeCategoryRepository(), new FakeTagRepository());

        var archived = await service.Archive(Guid.NewGuid());
        var restored = await service.Restore(Guid.NewGuid());

        Assert.Equal(ApplicationErrors.TagNotFound, archived.Error);
        Assert.Equal(ApplicationErrors.TagNotFound, restored.Error);
    }

    [Fact]
    public async Task Should_return_the_repository_failure_when_listing_archived_tags()
    {
        var tags = new FakeTagRepository { Failure = InfrastructureErrors.Unexpected };
        var service = new TagService(new FakeCategoryRepository(), tags);

        var result = await service.GetArchived(Guid.NewGuid());

        Assert.Equal(InfrastructureErrors.Unexpected, result.Error);
    }

    private static Tag ArchivedTag(Guid categoryId, string name)
    {
        var tag = new Tag(Guid.NewGuid(), categoryId, name);
        tag.Archive();
        return tag;
    }
}
