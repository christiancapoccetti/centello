using Centello.App.Controls;
using Centello.App.ViewModels;
using Centello.Application;
using Centello.Domain;

namespace Centello.App.Pages;

public partial class ExpenseEditorPage : ContentPage, IQueryAttributable, IHoldsUnsavedInput
{
    private const string DraftParameter = "draft";

    private readonly ExpenseEditorViewModel _viewModel;
    private readonly IConfirmation _confirmation;
    private readonly ISnackbar _snackbar;
    private const string ExpenseIdKey = "expenseId";
    private const string CategoryIdKey = "categoryId";
    private const string FromKey = "from";
    private const string Outside = "outside";
    private Guid? _expenseId;
    private Guid? _categoryId;
    private bool _fromOutside;
    private ExpenseDraft? _draft;
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
    /// already chosen. An editor opened from outside the app sends the person back to the home screen when it
    /// closes.</summary>
    public static string Route(Guid? expenseId = null, Guid? categoryId = null, bool fromOutside = false)
    {
        List<string> query = [];
        if (expenseId.HasValue)
            query.Add($"{ExpenseIdKey}={expenseId}");

        if (categoryId.HasValue)
            query.Add($"{CategoryIdKey}={categoryId}");

        if (fromOutside)
            query.Add($"{FromKey}={Outside}");

        return query.Count == 0
            ? nameof(ExpenseEditorPage)
            : $"{nameof(ExpenseEditorPage)}?{string.Join('&', query)}";
    }

    /// <summary>Opens a new expense filled from a draft, on the home tab whichever tab is in front. The draft travels
    /// with this one navigation only, so it can never reach a later "Aggiungi spesa".</summary>
    public static Task OpenDraft(ExpenseDraft draft, bool fromOutside = false)
    {
        var parameters = new ShellNavigationQueryParameters { [DraftParameter] = draft };
        if (fromOutside)
            parameters[FromKey] = Outside;

        return Shell.Current.GoToAsync($"//{nameof(TodayPage)}/{nameof(ExpenseEditorPage)}", parameters);
    }

    public bool HasUnsavedInput => _viewModel.IsDirty;

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (_isLoaded)
            return;

        _expenseId = GuidOf(query, ExpenseIdKey);
        _categoryId = GuidOf(query, CategoryIdKey);
        _fromOutside = query.TryGetValue(FromKey, out var from) && from?.ToString() == Outside;
        _draft = query.TryGetValue(DraftParameter, out var draft) ? draft as ExpenseDraft : null;
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
        await (_draft is { } draft ? _viewModel.Load(draft) : _viewModel.Load(_expenseId, _categoryId));
        // A new expense starts from its amount. A stored expense or a draft from the voice or a photo is there to be read first.
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

        await Pop();
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
        await Pop();
        if (!_fromOutside)
            _snackbar.Show(message);
    }

    // From outside nobody would see the snackbar: the person goes back to where they were.
    private async Task Pop()
    {
        await Shell.Current.GoToAsync("..");
        if (_fromOutside)
            BackToLauncher.Go();
    }
}
