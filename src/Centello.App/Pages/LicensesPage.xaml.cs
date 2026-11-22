using Centello.App.ViewModels;

namespace Centello.App.Pages;

public partial class LicensesPage : ContentPage
{
    private const string SourceUrl = "https://github.com/christiancapoccetti/centello";

    private readonly LicensesViewModel _viewModel;
    private readonly ISnackbar _snackbar;

    public LicensesPage(LicensesViewModel viewModel, ISnackbar snackbar)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _snackbar = snackbar;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.Load();
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private async void OnSourceClicked(object? sender, EventArgs e)
    {
        try
        {
            await Launcher.Default.OpenAsync(new Uri(SourceUrl));
        }
        catch
        {
            _snackbar.Show("Non è stato possibile aprire il browser");
        }
    }
}
