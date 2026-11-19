using Centesimo.App.Controls;
using Centesimo.App.ViewModels;
using Centesimo.Domain;

namespace Centesimo.App.Pages;

public partial class ExpenseEditorPage : ContentPage, IQueryAttributable
{
    private readonly ExpenseEditorViewModel _viewModel;
    private readonly CategoryEditorPage _categoryEditor;
    private readonly IConfirmation _confirmation;
    private readonly ISnackbar _snackbar;
    private Guid? _expenseId;
    private bool _isLoaded;

    public ExpenseEditorPage(ExpenseEditorViewModel viewModel, CategoryEditorPage categoryEditor,
        IConfirmation confirmation, ISnackbar snackbar)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _categoryEditor = categoryEditor;
        _confirmation = confirmation;
        _snackbar = snackbar;
        BindingContext = viewModel;
        viewModel.Saved += OnSaved;
        viewModel.Rejected += OnRejected;
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _expenseId = query.TryGetValue("expenseId", out var value)
            && Guid.TryParse(value?.ToString(), out var expenseId)
                ? expenseId
                : null;
    }

    // The page appears again when the category editor closes or the app returns to the front. Loading again would
    // throw away what was typed, so later appearances only refresh the categories on offer.
    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (_isLoaded)
        {
            await _viewModel.Picker.Refresh();
            return;
        }

        _isLoaded = true;
        await _viewModel.Load(_expenseId);
        // A new expense starts from its amount. A stored expense or a draft from the voice is there to be read first.
        if (!_viewModel.IsEditMode && _viewModel.Amount.IsEmpty())
            AmountInput.FocusInput();
    }

    protected override bool OnBackButtonPressed()
    {
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

    private async void OnCategoryTapped(object? sender, object category)
    {
        if (category is CategoryOption option)
            await _viewModel.Picker.Select(option);
    }

    private void OnTagTapped(object? sender, object tag)
    {
        if (tag is TagOption option)
            _viewModel.Picker.Toggle(option);
    }

    private async void OnNewCategoryTapped(object? sender, EventArgs e)
    {
        _categoryEditor.OpenNew();
        await Navigation.PushModalAsync(_categoryEditor, false);
    }

    private void OnMenuClicked(object? sender, EventArgs e) => MenuLayer.IsVisible = true;

    private void OnMenuDismissed(object? sender, TappedEventArgs e) => MenuLayer.IsVisible = false;

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        MenuLayer.IsVisible = false;
        var confirmed = await _confirmation.Ask("Eliminare questa spesa?", "Non si può annullare.", "Elimina", "Annulla",
            isDestructive: true);
        if (!confirmed)
            return;

        var deleted = await _viewModel.Delete();
        if (deleted.IsSuccess)
            await Leave("Spesa eliminata");
    }

    private async void OnSaved(object? sender, EventArgs e) => await Leave("Spesa salvata");

    private async void OnRejected(object? sender, EventArgs e) => await InlineError.ShowFirst(Form);

    private async Task Leave(string message)
    {
        await Shell.Current.GoToAsync("..");
        _snackbar.Show(message);
    }
}
