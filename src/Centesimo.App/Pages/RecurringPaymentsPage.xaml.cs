using Centesimo.App.ViewModels;
using Centesimo.Application;

namespace Centesimo.App.Pages;

public partial class RecurringPaymentsPage : ContentPage
{
    private readonly RecurringPaymentsViewModel _viewModel;

    public RecurringPaymentsPage(RecurringPaymentsViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnAddClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(nameof(RecurringPaymentEditorPage));

    private async void OnPaymentClicked(object? sender, EventArgs e)
    {
        if (sender is not Button { CommandParameter: RecurringPaymentRow payment })
            return;

        await Shell.Current.GoToAsync($"{nameof(RecurringPaymentEditorPage)}?paymentId={payment.RecurringPaymentId}");
    }

    private void OnEndedToggled(object? sender, EventArgs e) => _viewModel.ToggleEnded();
}
