using Centello.App.ViewModels;

namespace Centello.App.Pages;

public partial class RestorePage : ContentPage
{
    private readonly RestoreViewModel _viewModel;
    private CancellationTokenSource? _navigation;
    private bool _picking;

    public RestorePage(RestoreViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);
        _navigation = new CancellationTokenSource();
    }

    protected override void OnNavigatedFrom(NavigatedFromEventArgs args)
    {
        _navigation?.Cancel();
        _navigation?.Dispose();
        _navigation = null;
        _viewModel.Abandon();
        base.OnNavigatedFrom(args);
    }

    // Leaving while the data is being replaced would hide whether it finished.
    protected override bool OnBackButtonPressed() => _viewModel.IsBusy || base.OnBackButtonPressed();

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    // A tab keeps its own stack: leaving it for Oggi would bring this finished page back under Impostazioni.
    private async void OnTodayClicked(object? sender, EventArgs e)
    {
        await Navigation.PopToRootAsync(false);
        await Shell.Current.GoToAsync($"//{nameof(TodayPage)}");
    }

    private async void OnChooseClicked(object? sender, EventArgs e)
    {
        if (_navigation is null || _picking || _viewModel.IsBusy)
            return;

        var navigation = _navigation.Token;
        _picking = true;
        try
        {
            var file = await FilePicker.Default.PickAsync(new PickOptions { PickerTitle = "Scegli il file di backup" });
            if (file is null || navigation.IsCancellationRequested)
                return;

            await using var stream = await file.OpenReadAsync();
            await _viewModel.Analyse(file.FileName, stream, navigation);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
            if (!navigation.IsCancellationRequested)
                _viewModel.FailToOpen();
        }
        finally
        {
            _picking = false;
        }
    }

    private async void OnRestoreClicked(object? sender, EventArgs e)
    {
        if (_navigation is not null && !_picking)
            await _viewModel.Restore(_navigation.Token);
    }
}
