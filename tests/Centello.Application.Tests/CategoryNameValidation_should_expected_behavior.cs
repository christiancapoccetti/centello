using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class CategoryNameValidation_should_expected_behavior
{
    [Fact]
    public async Task Should_reject_duplicate_name_ignoring_case_and_whitespace()
    {
        var repository = new FakeCategoryRepository();
        repository.Items.Add(new Category(Guid.NewGuid(), "Food", "cart", "coral"));
        var service = new CategoryService(repository);

        var result = await service.Create(" food ", "home", "coral");

        Assert.True(result.IsFailure);
        Assert.Equal("Category.NameAlreadyExists", result.Error.Code);
        Assert.Single(repository.Items);
    }

    [Fact]
    public async Task Should_allow_unchanged_name_and_reject_another_category_name_on_update()
    {
        var repository = new FakeCategoryRepository();
        var food = new Category(Guid.NewGuid(), "Food", "cart", "coral");
        var travel = new Category(Guid.NewGuid(), "Travel", "car", "peri");
        repository.Items.AddRange([food, travel]);
        var service = new CategoryService(repository);

        var unchanged = await service.Update(food.CategoryId, "food", "home", "coral", null);
        var duplicate = await service.Update(food.CategoryId, "Travel", "home", "coral", null);

        Assert.True(unchanged.IsSuccess);
        Assert.True(duplicate.IsFailure);
        Assert.Equal("Category.NameAlreadyExists", duplicate.Error.Code);
    }

    [Fact]
    public async Task Should_propagate_repository_failure_during_duplicate_check()
    {
        var repository = new FakeCategoryRepository { Failure = InfrastructureErrors.Unexpected };
        var service = new CategoryService(repository);

        var result = await service.Create("Food", "cart", "coral");

        Assert.True(result.IsFailure);
        Assert.Equal("Infrastructure.Unexpected", result.Error.Code);
    }
}
