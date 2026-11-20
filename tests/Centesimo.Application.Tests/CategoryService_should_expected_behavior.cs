using Centesimo.Domain;

namespace Centesimo.Application.Tests;

public sealed class CategoryService_should_expected_behavior
{
    [Fact]
    public async Task Create_set_budget_and_archive()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);
        var created = await service.Create("Food", "cart", "#123456");

        await service.SetBudget(created.Value.CategoryId, new Money(20_000));
        await service.Archive(created.Value.CategoryId);

        Assert.Equal(20_000, created.Value.MonthlyBudget?.Cents);
        Assert.True(created.Value.IsArchived);
    }

    [Fact]
    public async Task Get_a_category_by_id_archived_or_not_and_nothing_for_an_unknown_id()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);
        var active = (await service.Create("Spesa", "cart", "coral")).Value;
        var archived = (await service.Create("Palestra", "gym", "mint")).Value;
        await service.Archive(archived.CategoryId);

        Assert.Equal("Spesa", (await service.Get(active.CategoryId)).Value?.Name);
        Assert.True((await service.Get(archived.CategoryId)).Value?.IsArchived);
        Assert.Null((await service.Get(Guid.NewGuid())).Value);
    }

    [Fact]
    public async Task Propagate_infrastructure_failure_instead_of_not_found()
    {
        var repository = new FakeCategoryRepository
        {
            Failure = InfrastructureErrors.Unexpected
        };
        var service = new CategoryService(repository);

        var result = await service.Archive(Guid.NewGuid());

        Assert.Equal("Infrastructure.Unexpected", result.Error.Code);
    }
    [Fact]
    public async Task Return_only_active_categories_and_update_details()
    {
        var repository = new FakeCategoryRepository();
        var active = new Category(Guid.NewGuid(), "Food", "cart", "#176B5B");
        var archived = new Category(Guid.NewGuid(), "Old", "more", "#6A4F88");
        archived.Archive();
        repository.Items.AddRange([active, archived]);
        var service = new CategoryService(repository);

        var updated = await service.Update(active.CategoryId, "Dining", "home", "#8B4A5D", new Money(5000));
        var categories = await service.GetActive();

        Assert.True(updated.IsSuccess);
        Assert.Single(categories.Value);
        Assert.Equal("Dining", categories.Value[0].Name);
    }

    [Fact]
    public async Task Return_only_archived_categories_ordered_by_name()
    {
        var repository = new FakeCategoryRepository();
        var first = new Category(Guid.NewGuid(), "Zaini", "cart", "#176B5B");
        first.Archive();
        var second = new Category(Guid.NewGuid(), "Auto", "car", "#6A4F88");
        second.Archive();
        repository.Items.AddRange([first, new Category(Guid.NewGuid(), "Cibo", "food", "#8B4A5D"), second]);
        var service = new CategoryService(repository);

        var categories = await service.GetArchived();

        Assert.Equal(["Auto", "Zaini"], categories.Value.Select(category => category.Name));
    }

    [Fact]
    public async Task Restore_archived_category()
    {
        var repository = new FakeCategoryRepository();
        var category = new Category(Guid.NewGuid(), "Cibo", "cart", "#176B5B");
        category.Archive();
        repository.Items.Add(category);
        var service = new CategoryService(repository);

        var result = await service.Restore(category.CategoryId);

        Assert.True(result.IsSuccess);
        Assert.False(category.IsArchived);
    }

    [Fact]
    public async Task Propagate_repository_failure_when_restoring_category()
    {
        var repository = new FakeCategoryRepository { Failure = InfrastructureErrors.Unexpected };
        var service = new CategoryService(repository);

        var result = await service.Restore(Guid.NewGuid());

        Assert.Equal("Infrastructure.Unexpected", result.Error.Code);
    }

    [Fact]
    public async Task Return_not_found_when_restoring_missing_category()
    {
        var service = new CategoryService(new FakeCategoryRepository());

        var result = await service.Restore(Guid.NewGuid());

        Assert.Equal("Category.NotFound", result.Error.Code);
    }

    [Fact]
    public async Task Create_a_category_together_with_its_tags()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);

        var created = await service.Create("Ristoranti", "food", "mint", new Money(15_000), ["Pizza", "Asporto"]);

        Assert.True(created.IsSuccess);
        Assert.Same(created.Value, Assert.Single(repository.Items));
        Assert.Equal(["Pizza", "Asporto"], repository.AddedTags.Select(tag => tag.Name));
        Assert.All(repository.AddedTags, tag => Assert.Equal(created.Value.CategoryId, tag.CategoryId));
        Assert.Equal(15_000, created.Value.MonthlyBudget?.Cents);
    }

    [Fact]
    public async Task Trim_tag_names_skip_blank_ones_and_collapse_duplicates_ignoring_case()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);

        await service.Create("Ristoranti", "food", "mint", null, ["  Pizza ", "", "   ", "pizza", "Asporto", "PIZZA", "Asporto "]);

        Assert.Equal(["Pizza", "Asporto"], repository.AddedTags.Select(tag => tag.Name));
    }

    [Fact]
    public async Task Create_without_tags_when_none_are_given()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);

        var withoutList = await service.Create("Casa", "home", "peri");
        var emptyList = await service.Create("Auto", "car", "butter", null, []);

        Assert.True(withoutList.IsSuccess && emptyList.IsSuccess);
        Assert.Empty(repository.AddedTags);
    }

    [Fact]
    public async Task Reject_a_blank_name_and_a_duplicate_name_without_creating_the_tags()
    {
        var repository = new FakeCategoryRepository();
        repository.Items.Add(new Category(Guid.NewGuid(), "Casa", "home", "peri"));
        var service = new CategoryService(repository);

        var blank = await service.Create("  ", "food", "mint", null, ["Pizza"]);
        var duplicate = await service.Create(" casa ", "food", "mint", null, ["Pizza"]);

        Assert.Equal("Validation.InvalidName", blank.Error.Code);
        Assert.Equal("Category.NameAlreadyExists", duplicate.Error.Code);
        Assert.Single(repository.Items);
        Assert.Empty(repository.AddedTags);
    }

    [Fact]
    public async Task Propagate_repository_failure_when_creating_a_category_with_tags()
    {
        var repository = new FakeCategoryRepository { Failure = InfrastructureErrors.Unexpected };
        var service = new CategoryService(repository);

        var result = await service.Create("Ristoranti", "food", "mint", null, ["Pizza"]);

        Assert.Equal("Infrastructure.Unexpected", result.Error.Code);
    }

    [Fact]
    public async Task Create_the_suggested_categories_without_budgets()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);

        var result = await service.CreateSuggested();

        Assert.True(result.IsSuccess);
        Assert.Equal(["Spesa", "Casa", "Trasporti", "Ristoranti", "Salute", "Svago"], result.Value.Select(category => category.Name));
        Assert.Equal(result.Value, repository.Items);
        Assert.Equal(["cart", "home", "car", "food", "health", "leisure"], repository.Items.Select(category => category.Icon));
        Assert.Equal(["coral", "peri", "butter", "mint", "sky", "lilac"], repository.Items.Select(category => category.Color));
        Assert.All(repository.Items, category => Assert.Null(category.MonthlyBudget));
        Assert.Empty(repository.AddedTags);
    }

    [Fact]
    public async Task Create_no_suggested_category_the_second_time()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);
        await service.CreateSuggested();

        var second = await service.CreateSuggested();

        Assert.True(second.IsSuccess);
        Assert.Empty(second.Value);
        Assert.Equal(6, repository.Items.Count);
    }

    [Fact]
    public async Task Skip_suggested_names_that_exist_ignoring_case_and_archived_ones()
    {
        var repository = new FakeCategoryRepository();
        var archived = new Category(Guid.NewGuid(), "SPESA", "more", "grey");
        archived.Archive();
        repository.Items.AddRange([archived, new Category(Guid.NewGuid(), "casa", "gear", "lilac")]);
        var service = new CategoryService(repository);

        var result = await service.CreateSuggested();

        Assert.Equal(["Trasporti", "Ristoranti", "Salute", "Svago"], result.Value.Select(category => category.Name));
        Assert.Equal(6, repository.Items.Count);
        Assert.Equal("more", repository.Items.Single(category => category.Name == "SPESA").Icon);
    }

    [Fact]
    public async Task Create_only_the_chosen_suggestions_in_catalog_order()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);

        var result = await service.CreateSuggested(["svago", "Casa", "CASA"]);

        Assert.Equal(["Casa", "Svago"], result.Value.Select(category => category.Name));
        Assert.Equal(["Casa", "Svago"], repository.Items.Select(category => category.Name));
    }

    [Fact]
    public async Task Ignore_chosen_names_the_suggestions_do_not_include()
    {
        var repository = new FakeCategoryRepository();
        var service = new CategoryService(repository);

        var unknown = await service.CreateSuggested(["Animali"]);
        var none = await service.CreateSuggested([]);

        Assert.Empty(unknown.Value);
        Assert.Empty(none.Value);
        Assert.Empty(repository.Items);
    }

    [Fact]
    public async Task Propagate_repository_failure_when_creating_suggested_categories()
    {
        var unreadable = new FakeCategoryRepository { Failure = InfrastructureErrors.Unexpected };
        var unwritable = new FakeCategoryRepository { AddFailure = InfrastructureErrors.PersistenceFailure };

        var readFailure = await new CategoryService(unreadable).CreateSuggested();
        var writeFailure = await new CategoryService(unwritable).CreateSuggested();

        Assert.Equal("Infrastructure.Unexpected", readFailure.Error.Code);
        Assert.Equal(InfrastructureErrors.PersistenceFailure, writeFailure.Error);
        Assert.Empty(unwritable.Items);
    }
}


