using Centello.App.Controls;
using Centello.App.ViewModels;

namespace Centello.App.Pages;

public partial class CategoryEditorPage : ContentPage, IQueryAttributable, IHoldsUnsavedInput
{
    private readonly CategoryEditorViewModel _viewModel;
    private readonly IConfirmation _confirmation;
    private readonly ISnackbar _snackbar;
    private Guid? _categoryId;
    private bool _isLoaded;

    public CategoryEditorPage(CategoryEditorViewModel viewModel, IConfirmation confirmation, ISnackbar snackbar)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _confirmation = confirmation;
        _snackbar = snackbar;
        BindingContext = viewModel;
        viewModel.Saved += OnSaved;
        viewModel.Rejected += OnRejected;
    }

    /// <summary>The route that opens a stored category, or the empty form of a new one without an id.</summary>
    public static string Route(Guid? categoryId = null) => categoryId.HasValue
        ? $"{nameof(CategoryEditorPage)}?categoryId={categoryId}"
        : nameof(CategoryEditorPage);

    public bool HasUnsavedInput => _viewModel.IsDirty;

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _categoryId = query.TryGetValue("categoryId", out var value) && Guid.TryParse(value?.ToString(), out var id)
            ? id
            : null;

    // The page appears again when the app returns to the front. Loading again would throw away what was typed.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isLoaded)
            return;

        _isLoaded = true;
        await _viewModel.Load(_categoryId);
    }

    protected override bool OnBackButtonPressed()
    {
        if (SoftKeyboard.Dismiss(this))
            return true;

        if (MenuLayer.IsVisible)
            MenuLayer.IsVisible = false;
        else
            _ = Close();

        return true;
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Close();

    private async Task Close()
    {
        if (_viewModel.IsDirty && !await _confirmation.AskToDiscard())
            return;

        await Shell.Current.GoToAsync("..");
    }

    private void OnMenuClicked(object? sender, EventArgs e) => MenuLayer.IsVisible = true;

    private void OnMenuDismissed(object? sender, TappedEventArgs e) => MenuLayer.IsVisible = false;

    private async void OnArchiveClicked(object? sender, EventArgs e)
    {
        MenuLayer.IsVisible = false;
        var confirmed = await _confirmation.Ask(_viewModel.ArchiveQuestion,
            "Le spese restano, la categoria non sarà più proposta.", "Archivia", "Annulla");
        if (!confirmed)
            return;

        var archived = await _viewModel.Archive();
        if (archived.IsSuccess)
            await Leave(_viewModel.Done("archiviata"));
    }

    private async void OnRestoreClicked(object? sender, EventArgs e)
    {
        var restored = await _viewModel.Restore();
        if (restored.IsSuccess)
            await Leave(_viewModel.Done("ripristinata"));
    }

    private async void OnTagChipTapped(object? sender, EventArgs e)
    {
        if (sender is not BindableObject { BindingContext: EditorTagChip chip })
            return;

        switch (chip.Kind)
        {
            case EditorTagChipKind.Add:
                _viewModel.StartAddingTag();
                TagField.FocusInput();
                break;
            case EditorTagChipKind.Tag when chip.TagId.HasValue:
                await ArchiveTag(chip);
                break;
            case EditorTagChipKind.Tag:
                _viewModel.RemovePendingTag(chip);
                break;
            case EditorTagChipKind.Archived:
                if ((await _viewModel.RestoreTag(chip)).IsSuccess)
                    _snackbar.Show($"Tag {chip.Name} ripristinato");
                break;
        }
    }

    private async Task ArchiveTag(EditorTagChip chip)
    {
        var confirmed = await _confirmation.Ask($"Archiviare il tag {chip.Name}?",
            "Le spese restano, il tag non sarà più proposto.", "Archivia", "Annulla");
        if (confirmed && (await _viewModel.ArchiveTag(chip)).IsSuccess)
            _snackbar.Show($"Tag {chip.Name} archiviato");
    }

    private async void OnAddTagConfirmed(object? sender, EventArgs e) => await _viewModel.AddTag();

    private async void OnSaved(object? sender, EventArgs e) => await Leave(_viewModel.Done("salvata"));

    private async void OnRejected(object? sender, EventArgs e) => await Form.ShowFirstError();

    private async Task Leave(string message)
    {
        await Shell.Current.GoToAsync("..");
        _snackbar.Show(message);
    }
}
