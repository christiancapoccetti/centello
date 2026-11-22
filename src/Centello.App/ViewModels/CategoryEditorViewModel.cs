using System.Globalization;
using System.Windows.Input;
using Centello.Application;
using Centello.Domain;

namespace Centello.App.ViewModels;

public enum EditorTagChipKind
{
    /// <summary>A tag of the category. A tap takes it away.</summary>
    Tag,
    /// <summary>A tag of a category that cannot change. A tap does nothing.</summary>
    Fixed,
    /// <summary>The chip that opens the field for a new tag.</summary>
    Add,
    /// <summary>An archived tag. A tap restores it.</summary>
    Archived
}

/// <summary>A chip of the tag rows of the category editor. A tag without an id is not stored yet: it is saved with
/// its new category.</summary>
public sealed record EditorTagChip(EditorTagChipKind Kind, string Name, Guid? TagId = null)
{
    public string Text => Kind == EditorTagChipKind.Add ? "Aggiungi tag" : Name;
    public string Glyph => Kind == EditorTagChipKind.Add ? MaterialSymbols.Add : "";
    public string TrailingGlyph => Kind switch
    {
        EditorTagChipKind.Tag => MaterialSymbols.Close,
        EditorTagChipKind.Archived => MaterialSymbols.Restore,
        _ => ""
    };
    public string Description => Kind switch
    {
        EditorTagChipKind.Tag => TagId.HasValue ? $"Archivia il tag {Name}" : $"Togli il tag {Name}",
        EditorTagChipKind.Archived => $"Ripristina il tag {Name}",
        _ => Text
    };
}

public sealed class CategoryEditorViewModel : ObservableObject
{
    private static readonly CultureInfo ItalianCulture = CultureInfo.GetCultureInfo("it-IT");
    private readonly CategoryService _categoryService;
    private readonly TagService _tagService;
    private Guid? _categoryId;
    private Mode _mode = Mode.New;
    private string _storedName = "";
    private string _name = "";
    private string _selectedIcon = CategoryIcons.All[0].Key;
    private string _colorKey = CategoryPalette.Chromatic[0].Key;
    private string _budget = "";
    private string _newTagName = "";
    private bool _isAddingTag;
    private IReadOnlyList<EditorTagChip> _tagChips = [];
    private IReadOnlyList<EditorTagChip> _archivedTagChips = [];
    private string _nameError = "";
    private string _budgetError = "";
    private string _tagError = "";
    private string _errorMessage = "";
    private bool _isSaving;
    private FormState _original;

    public event EventHandler? Saved;
    /// <summary>The save stopped on a field. Its message is under that field.</summary>
    public event EventHandler? Rejected;

    public ICommand SaveCommand { get; }
    public string Title => _mode.Title;
    public bool CanEdit => _mode.CanEdit;
    public bool IsReadOnly => !_mode.CanEdit;
    public bool CanArchive => _mode.CanArchive;
    public bool IsArchived => _mode.CanRestore;
    /// <summary>The category is not stored yet.</summary>
    public bool IsNew => !_categoryId.HasValue;
    /// <summary>A new category has no tags to store them under yet: they wait for its first save.</summary>
    public bool SavesTagsWithCategory => IsNew;
    public string Name
    {
        get => _name;
        set
        {
            if (!SetProperty(ref _name, value))
                return;

            NameError = "";
            OnPropertyChanged(nameof(PreviewName));
            OnPropertyChanged(nameof(HasName));
        }
    }
    /// <summary>What the preview writes beside the tile: the name, or what goes there while it is empty.</summary>
    public string PreviewName => HasName ? Name.Trim() : "Nome della categoria";
    public bool HasName => Name.HasValue();
    public string SelectedIcon { get => _selectedIcon; set => SetProperty(ref _selectedIcon, value); }
    /// <summary>The color the category has, or will be given when it is saved. A new category starts on the color
    /// suggested by <see cref="CategoryService.NextColor"/>, a stored one on its own.</summary>
    public string ColorKey { get => _colorKey; set => SetProperty(ref _colorKey, value); }
    public string Budget
    {
        get => _budget;
        set
        {
            if (SetProperty(ref _budget, value))
                BudgetError = "";
        }
    }
    public string NewTagName
    {
        get => _newTagName;
        set
        {
            if (SetProperty(ref _newTagName, value))
                TagError = "";
        }
    }
    public bool IsAddingTag { get => _isAddingTag; private set => SetProperty(ref _isAddingTag, value); }
    /// <summary>The tags of the category and, while it can change, the chip that adds one.</summary>
    public IReadOnlyList<EditorTagChip> TagChips
    {
        get => _tagChips;
        private set
        {
            if (SetProperty(ref _tagChips, value))
                OnPropertyChanged(nameof(HasNoTags));
        }
    }
    public IReadOnlyList<EditorTagChip> ArchivedTagChips
    {
        get => _archivedTagChips;
        private set
        {
            if (SetProperty(ref _archivedTagChips, value))
                OnPropertyChanged(nameof(HasArchivedTags));
        }
    }
    public bool HasNoTags => TagChips.Count == 0;
    public bool HasArchivedTags => ArchivedTagChips.Count > 0;
    public string NameError { get => _nameError; private set => SetProperty(ref _nameError, value); }
    public string BudgetError { get => _budgetError; private set => SetProperty(ref _budgetError, value); }
    public string TagError { get => _tagError; private set => SetProperty(ref _tagError, value); }
    /// <summary>A failure that belongs to no field, such as a database error.</summary>
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public bool IsSaving { get => _isSaving; private set => SetProperty(ref _isSaving, value); }
    /// <summary>True while the form differs from what is stored, or from the empty form of a new category. The tags
    /// of a stored category are saved one by one as they change, so only a tag still being typed counts.</summary>
    public bool IsDirty => CanEdit && Snapshot() != _original;
    public string ArchiveQuestion => $"Archiviare {_storedName}?";

    public CategoryEditorViewModel(CategoryService categoryService, TagService tagService)
    {
        _categoryService = categoryService;
        _tagService = tagService;
        SaveCommand = new AsyncCommand(Save, () => !IsSaving);
        ShowTags([]);
        _original = Snapshot();
    }

    public async Task Load(Guid? categoryId)
    {
        if (categoryId.HasValue)
            await LoadCategory(categoryId.Value);
        else
            await LoadNextColor();

        _original = Snapshot();
    }

    /// <summary>"Ristoranti archiviata": what a snackbar says after something was done to the category.</summary>
    public string Done(string what) => $"{_storedName} {what}";

    public Task<Result> Archive() => Change(_categoryService.Archive);

    public Task<Result> Restore() => Change(_categoryService.Restore);

    public void StartAddingTag() => IsAddingTag = true;

    /// <summary>Adds the tag being typed. A new category keeps it until its first save. A stored one saves it at
    /// once, and a name that is archived comes back instead of being stored twice.</summary>
    public async Task AddTag()
    {
        var name = NewTagName.Trim();
        if (name.IsEmpty())
            return;

        var isNew = !TagChips.Any(chip => chip.Kind == EditorTagChipKind.Tag && Same(chip.Name, name));
        if (isNew && _categoryId is { } categoryId)
        {
            var archived = ArchivedTagChips.FirstOrDefault(chip => Same(chip.Name, name));
            var stored = archived?.TagId is { } tagId
                ? await _tagService.Restore(tagId)
                : await Create(categoryId, name);
            if (stored.IsFailure)
            {
                TagError = stored.Error.Message;
                return;
            }

            await LoadTags(categoryId);
        }
        else if (isNew)
            ShowTags([.. PendingTags, name]);

        NewTagName = "";
        IsAddingTag = false;
    }

    /// <summary>Takes a tag that was never stored off the new category.</summary>
    public void RemovePendingTag(EditorTagChip chip) =>
        ShowTags(PendingTags.Where(name => name != chip.Name).ToList());

    public Task<Result> ArchiveTag(EditorTagChip chip) => ChangeTag(chip, _tagService.Archive);

    public Task<Result> RestoreTag(EditorTagChip chip) => ChangeTag(chip, _tagService.Restore);

    private IEnumerable<string> PendingTags => TagChips
        .Where(chip => chip.Kind == EditorTagChipKind.Tag && !chip.TagId.HasValue)
        .Select(chip => chip.Name);

    private static bool Same(string first, string second) =>
        first.Equals(second, StringComparison.OrdinalIgnoreCase);

    private async Task LoadCategory(Guid categoryId)
    {
        var found = await _categoryService.Get(categoryId);
        if (found.IsFailure || found.Value is not { } category)
        {
            ErrorMessage = found.IsFailure ? found.Error.Message : ApplicationErrors.CategoryNotFound.Message;
            return;
        }

        _categoryId = categoryId;
        _mode = category.IsArchived ? Mode.Archived : Mode.Edit;
        _storedName = category.Name;
        Name = category.Name;
        SelectedIcon = CategoryIcons.Resolve(category.Icon).Key;
        // The stored key as it is: a grey or legacy one the palette cannot offer goes back to Update unchanged.
        ColorKey = category.Color;
        Budget = category.MonthlyBudget?.ToDecimal().ToString("0.00", ItalianCulture) ?? "";
        await LoadTags(categoryId);
        // Every property that reads the mode changes with it.
        OnPropertyChanged("");
    }

    private async Task LoadNextColor()
    {
        var next = await _categoryService.NextColor();
        if (next.IsSuccess)
            ColorKey = next.Value;
        else
            ErrorMessage = next.Error.Message;
    }

    private async Task LoadTags(Guid categoryId)
    {
        var active = await _tagService.GetActive(categoryId);
        var archived = await _tagService.GetArchived(categoryId);
        if (active.IsFailure || archived.IsFailure)
        {
            ErrorMessage = active.IsFailure ? active.Error.Message : archived.Error.Message;
            return;
        }

        var kind = CanEdit ? EditorTagChipKind.Tag : EditorTagChipKind.Fixed;
        TagChips = WithAdd(active.Value.Select(tag => new EditorTagChip(kind, tag.Name, tag.TagId)));
        // An archived category shows what it has. Its archived tags come back only once it is in use again.
        ArchivedTagChips = CanEdit
            ? archived.Value.Select(tag => new EditorTagChip(EditorTagChipKind.Archived, tag.Name, tag.TagId)).ToList()
            : [];
    }

    private void ShowTags(IReadOnlyList<string> pending) =>
        TagChips = WithAdd(pending.Select(name => new EditorTagChip(EditorTagChipKind.Tag, name)));

    private IReadOnlyList<EditorTagChip> WithAdd(IEnumerable<EditorTagChip> tags) => CanEdit
        ? [.. tags, new EditorTagChip(EditorTagChipKind.Add, "")]
        : tags.ToList();

    private async Task<Result> Create(Guid categoryId, string name)
    {
        var created = await _tagService.Create(categoryId, name);
        return created.IsSuccess ? Result.Success() : Result.Failure(created.Error);
    }

    private async Task<Result> ChangeTag(EditorTagChip chip, Func<Guid, CancellationToken, Task<Result>> change)
    {
        if (chip.TagId is not { } tagId || _categoryId is not { } categoryId)
            return Result.Failure(ApplicationErrors.TagNotFound);

        var result = await change(tagId, CancellationToken.None);
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
            await LoadTags(categoryId);

        return result;
    }

    private async Task<Result> Change(Func<Guid, CancellationToken, Task<Result>> change)
    {
        var result = await change(_categoryId.GetValueOrDefault(), CancellationToken.None);
        if (result.IsFailure)
            ErrorMessage = result.Error.Message;

        return result;
    }

    private async Task Save()
    {
        // A tag that was typed but not confirmed is still part of what the person sees, so it is not dropped.
        if (NewTagName.HasValue())
            await AddTag();

        var budget = MoneyInputParser.ParseOptional(Budget, ItalianCulture);
        // Both checks run, so every field that stops the save says so at once.
        NameError = Name.IsEmpty() ? ApplicationErrors.InvalidName.Message : "";
        BudgetError = budget.IsFailure ? budget.Error.Message : "";
        ErrorMessage = "";
        if (NameError.HasValue() || BudgetError.HasValue() || TagError.HasValue())
        {
            Rejected?.Invoke(this, EventArgs.Empty);
            return;
        }

        IsSaving = true;
        var result = _categoryId.HasValue
            ? await _categoryService.Update(_categoryId.Value, Name, SelectedIcon, ColorKey, budget.Value)
            : await CreateCategory(budget.Value);
        if (result.IsFailure)
        {
            IsSaving = false;
            Explain(result.Error);
            Rejected?.Invoke(this, EventArgs.Empty);
            return;
        }

        _storedName = Name.Trim();
        // Stays true after success so the save button cannot fire again while the page is popping.
        Saved?.Invoke(this, EventArgs.Empty);
    }

    private async Task<Result> CreateCategory(Money? budget)
    {
        var created = await _categoryService.Create(Name, SelectedIcon, ColorKey, budget, PendingTags.ToList());
        return created.IsSuccess ? Result.Success() : Result.Failure(created.Error);
    }

    private void Explain(Error error)
    {
        if (error == ApplicationErrors.InvalidName || error == ApplicationErrors.CategoryNameAlreadyExists)
            NameError = error.Message;
        else
            ErrorMessage = error.Message;
    }

    private FormState Snapshot() =>
        new(Name.Trim(), SelectedIcon, ColorKey, Budget.Trim(), string.Join('\n', PendingTags), NewTagName.Trim());

    private readonly record struct FormState(string Name, string Icon, string Color, string Budget, string PendingTags, string TypedTag);

    /// <summary>What the editor says and allows for a new category, a stored one and an archived one.</summary>
    private sealed record Mode(string Title, bool CanEdit, bool CanArchive, bool CanRestore)
    {
        public static readonly Mode New = new("Nuova categoria", CanEdit: true, CanArchive: false, CanRestore: false);
        public static readonly Mode Edit = new("Modifica categoria", CanEdit: true, CanArchive: true, CanRestore: false);
        public static readonly Mode Archived = new("Categoria archiviata", CanEdit: false, CanArchive: false, CanRestore: true);
    }
}
