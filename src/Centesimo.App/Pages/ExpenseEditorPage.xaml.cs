using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class ExpenseEditorPage : ContentPage, IQueryAttributable
{
    private readonly ExpenseEditorViewModel _viewModel;
    private readonly CategoryEditorPage _categoryEditor;
    private Guid? _expenseId;
    private bool _isLoaded;

    public ExpenseEditorPage(ExpenseEditorViewModel viewModel, CategoryEditorPage categoryEditor)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _categoryEditor = categoryEditor;
        BindingContext = viewModel;
        viewModel.Saved += OnSaved;
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

    private async void OnDeleteClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlertAsync(
            "Elimina spesa",
            "Vuoi eliminare questa spesa? L'azione non può essere annullata.",
            "Elimina",
            "Annulla");
        if (!confirmed)
            return;

        await _viewModel.Delete();
    }

    private async void OnSaved(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");

    private async void OnCancelClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync("..");
}
