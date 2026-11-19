using Centesimo.App.ViewModels;

namespace Centesimo.App.Pages;

public partial class RecurringPaymentEditorPage : ContentPage, IQueryAttributable
{
    private readonly RecurringPaymentEditorViewModel _viewModel;
    private readonly RecurringPaymentAutomation _automation;
    private readonly CategoryEditorPage _categoryEditor;
    private Guid? _paymentId;
    private bool _isLoaded;

    public RecurringPaymentEditorPage(RecurringPaymentEditorViewModel viewModel,
        RecurringPaymentAutomation automation, CategoryEditorPage categoryEditor)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _automation = automation;
        _categoryEditor = categoryEditor;
        BindingContext = viewModel;
        viewModel.Saved += OnSaved;
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

    private async void OnCloseClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnSaved(object? sender, EventArgs e)
    {
        await RequestNotificationPermission();
        await Shell.Current.GoToAsync("..");
    }

    private async Task RequestNotificationPermission()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        if (status == PermissionStatus.Granted)
            return;

        var accepted = await DisplayAlertAsync(
            "Promemoria",
            "Centesimo può inviarti una notifica per ricordarti le prossime scadenze dei pagamenti regolari.",
            "Consenti",
            "Non ora");
        if (!accepted)
            return;

        if (await Permissions.RequestAsync<Permissions.PostNotifications>() == PermissionStatus.Granted)
            await _automation.ProcessDue();
    }
}
