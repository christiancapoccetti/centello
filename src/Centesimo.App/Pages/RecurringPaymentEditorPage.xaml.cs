using Centesimo.App.Controls;
using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class RecurringPaymentEditorPage : ContentPage, IQueryAttributable
{
    private readonly RecurringPaymentEditorViewModel _viewModel;
    private readonly CategoryEditorPage _categoryEditor;
    private readonly IConfirmation _confirmation;
    private readonly ISnackbar _snackbar;
    private Guid? _paymentId;
    private bool _isLoaded;

    public RecurringPaymentEditorPage(RecurringPaymentEditorViewModel viewModel, CategoryEditorPage categoryEditor,
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

    public void ApplyQueryAttributes(IDictionary<string, object> query) =>
        _paymentId = query.TryGetValue("paymentId", out var value) &&
            Guid.TryParse(value?.ToString(), out var id)
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
        await _viewModel.Load(_paymentId);
    }

    protected override bool OnBackButtonPressed()
    {
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

    private async void OnReminderClicked(object? sender, EventArgs e)
    {
        if (await _viewModel.AcceptReminder())
            _snackbar.Show("Promemoria attivi: ti avviso il giorno della spesa");
    }

    private async void OnPauseClicked(object? sender, EventArgs e)
    {
        var paused = await _viewModel.Pause();
        if (paused.IsSuccess)
            await Leave(_viewModel.Done("in pausa"));
    }

    private async void OnResumeClicked(object? sender, EventArgs e)
    {
        var resumed = await _viewModel.Resume();
        if (resumed.IsSuccess)
            await Leave(_viewModel.Done("ripreso"));
    }

    private async void OnEndClicked(object? sender, EventArgs e)
    {
        var preview = await _viewModel.PreviewEnd();
        if (preview.IsFailure)
            return;

        var confirmed = await _confirmation.Ask(_viewModel.EndQuestion, preview.Value.Message, "Termina", "Annulla",
            isDestructive: true);
        if (!confirmed)
            return;

        var ended = await _viewModel.End(preview.Value);
        if (ended.IsSuccess)
            await Leave(_viewModel.Done("terminato"));
    }

    private async void OnSaved(object? sender, EventArgs e) => await Leave("Pagamento salvato");

    private async void OnRejected(object? sender, EventArgs e) => await InlineError.ShowFirst(Form);

    private async Task Leave(string message)
    {
        await Shell.Current.GoToAsync("..");
        _snackbar.Show(message);
    }
}
