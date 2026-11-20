using Centesimo.App.Controls;
using Centesimo.App.ViewModels;
using Centesimo.Domain;

namespace Centesimo.App.Pages;

public partial class ExpenseEditorPage : ContentPage, IQueryAttributable
{
    private readonly ExpenseEditorViewModel _viewModel;
    private readonly IConfirmation _confirmation;
    private readonly ISnackbar _snackbar;
    private const string ExpenseIdKey = "expenseId";
    private const string CategoryIdKey = "categoryId";
    private Guid? _expenseId;
    private Guid? _categoryId;
    private bool _isLoaded;

    public ExpenseEditorPage(ExpenseEditorViewModel viewModel, IConfirmation confirmation, ISnackbar snackbar)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _confirmation = confirmation;
        _snackbar = snackbar;
        BindingContext = viewModel;
        viewModel.Saved += OnSaved;
        viewModel.Rejected += OnRejected;
    }

    /// <summary>The route that opens a stored expense, or the empty form of a new one, optionally with a category
    /// already chosen.</summary>
    public static string Route(Guid? expenseId = null, Guid? categoryId = null)
    {
        if (expenseId.HasValue)
            return $"{nameof(ExpenseEditorPage)}?{ExpenseIdKey}={expenseId}";

        return categoryId.HasValue
            ? $"{nameof(ExpenseEditorPage)}?{CategoryIdKey}={categoryId}"
            : nameof(ExpenseEditorPage);
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        _expenseId = GuidOf(query, ExpenseIdKey);
        _categoryId = GuidOf(query, CategoryIdKey);
    }

    private static Guid? GuidOf(IDictionary<string, object> query, string key) =>
        query.TryGetValue(key, out var value) && Guid.TryParse(value?.ToString(), out var id)
            ? id
            : null;

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
        await _viewModel.Load(_expenseId, _categoryId);
        // A new expense starts from its amount. A stored expense or a draft from the voice is there to be read first.
        if (!_viewModel.IsEditMode && _viewModel.Amount.IsEmpty())
            AmountInput.FocusInput();
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

    private async void OnNewCategoryTapped(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(CategoryEditorPage.Route());

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
