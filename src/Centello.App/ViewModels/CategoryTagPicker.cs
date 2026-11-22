using System.Collections.ObjectModel;
using Centello.Application;
using Centello.Domain;

namespace Centello.App.ViewModels;

public sealed class CategoryOption(Guid categoryId, string name, string icon, string color) : ObservableObject
{
    private bool _isSelected;

    public Guid CategoryId { get; } = categoryId;
    public string Name { get; } = name;
    public string Icon { get; } = icon;
    public string Color { get; } = color;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}

public sealed class TagOption(Guid tagId, string name) : ObservableObject
{
    private bool _isSelected;

    public Guid TagId { get; } = tagId;
    public string Name { get; } = name;
    public bool IsSelected { get => _isSelected; set => SetProperty(ref _isSelected, value); }
}

/// <summary>The category and the tag an editor asks for, with the message that belongs under each. Both editors own
/// one, so they offer, restore and check the choice the same way. The options hold the selection: at most one
/// category and one tag are selected.</summary>
public sealed class CategoryTagPicker(CategoryChoiceService categoryChoices, TagService tagService) : ObservableObject
{
    private const string ChooseCategoryFirst = "Scegli prima una categoria";
    private Guid? _alsoOffer;
    private string _tagHint = ChooseCategoryFirst;
    private string _categoryError = "";
    private string _tagError = "";

    public ObservableCollection<CategoryOption> Categories { get; } = [];
    public ObservableCollection<TagOption> Tags { get; } = [];
    public Guid? CategoryId => SelectedCategory?.CategoryId;
    public Guid? TagId => Tags.FirstOrDefault(tag => tag.IsSelected)?.TagId;
    public string TagHint { get => _tagHint; private set => SetProperty(ref _tagHint, value); }
    public string CategoryError { get => _categoryError; private set => SetProperty(ref _categoryError, value); }
    public string TagError { get => _tagError; private set => SetProperty(ref _tagError, value); }

    private CategoryOption? SelectedCategory => Categories.FirstOrDefault(category => category.IsSelected);

    /// <summary>Loads the categories on offer and keeps the selection. <paramref name="alsoOffer"/> adds a category
    /// that may be archived, for an editor that only shows what was stored.</summary>
    public Task<Result> Load(Guid? alsoOffer = null)
    {
        _alsoOffer = alsoOffer;
        return LoadCategories();
    }

    /// <summary>Loads the offer again when the editor comes back to the front. A category created meanwhile is the
    /// one the person wants, so the only new category becomes the selection.</summary>
    public async Task Refresh()
    {
        var known = Categories.Select(category => category.CategoryId).ToHashSet();
        var loaded = await LoadCategories();
        if (loaded.IsFailure)
            return;

        var created = Categories.Where(category => !known.Contains(category.CategoryId)).ToList();
        if (created.Count == 1)
            await Select(created[0]);
    }

    public async Task Select(CategoryOption category)
    {
        if (category.IsSelected)
            return;

        Mark(category);
        CategoryError = "";
        await LoadTags(null);
    }

    public void Toggle(TagOption tag)
    {
        var select = !tag.IsSelected;
        foreach (var option in Tags)
            option.IsSelected = select && option == tag;

        TagError = "";
    }

    /// <summary>Selects the category and the tag of a stored expense or payment. One that is no longer on offer stays
    /// unselected and says why under its field.</summary>
    public async Task Restore(Guid categoryId, Guid? tagId)
    {
        var category = Categories.FirstOrDefault(option => option.CategoryId == categoryId);
        if (category is null)
        {
            CategoryError = "La categoria originale non è più attiva. Seleziona una nuova categoria.";
            return;
        }

        Mark(category);
        await LoadTags(tagId);
        if (tagId.HasValue && !TagId.HasValue && TagError.IsEmpty())
            TagError = "Il tag originale non è più attivo. Scegli un tag disponibile o salva senza tag.";
    }

    /// <summary>Selects what a draft proposes. A proposal that is not on offer is dropped: the person chooses.</summary>
    public async Task Suggest(Guid? categoryId, Guid? tagId)
    {
        var category = Categories.FirstOrDefault(option => option.CategoryId == categoryId);
        if (category is null)
            return;

        Mark(category);
        await LoadTags(tagId);
    }

    public bool Validate()
    {
        if (SelectedCategory is null)
            CategoryError = "Seleziona una categoria.";

        return SelectedCategory is not null;
    }

    /// <summary>Puts a failure about the category or the tag under its field. False for any other failure.</summary>
    public bool Explain(Error error)
    {
        if (error == ApplicationErrors.CategoryNotFound || error == ApplicationErrors.CategoryArchived)
        {
            CategoryError = error.Message;
            return true;
        }

        if (error != ApplicationErrors.TagNotFound && error != ApplicationErrors.TagArchived && error != ApplicationErrors.TagCategoryMismatch)
            return false;

        TagError = error.Message;
        return true;
    }

    private async Task<Result> LoadCategories()
    {
        var result = await categoryChoices.Get(_alsoOffer);
        if (result.IsFailure)
            return result;

        // The grid redraws on every change of the collection, so an unchanged offer is left alone.
        var unchanged = result.Value
            .Select(category => (category.CategoryId, category.Name, category.Icon, category.Color))
            .SequenceEqual(Categories.Select(option => (option.CategoryId, option.Name, option.Icon, option.Color)));
        if (unchanged)
            return result;

        var selected = CategoryId;
        Categories.Clear();
        foreach (var category in result.Value)
        {
            Categories.Add(new CategoryOption(category.CategoryId, category.Name, category.Icon, category.Color)
            {
                IsSelected = category.CategoryId == selected
            });
        }

        if (selected.HasValue && !CategoryId.HasValue)
            await LoadTags(null);

        return result;
    }

    private void Mark(CategoryOption category)
    {
        foreach (var option in Categories)
            option.IsSelected = option == category;
    }

    private async Task LoadTags(Guid? selectedTagId)
    {
        Tags.Clear();
        TagError = "";
        if (SelectedCategory is not { } category)
        {
            TagHint = ChooseCategoryFirst;
            return;
        }

        var result = await tagService.GetActive(category.CategoryId);
        if (result.IsFailure)
        {
            TagHint = "";
            TagError = result.Error.Message;
            return;
        }

        foreach (var tag in result.Value)
            Tags.Add(new TagOption(tag.TagId, tag.Name) { IsSelected = tag.TagId == selectedTagId });

        TagHint = Tags.Count == 0 ? $"{category.Name} non ha tag" : "";
    }
}
