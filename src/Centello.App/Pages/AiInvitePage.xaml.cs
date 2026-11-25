using System.Diagnostics;
using Centello.App.Controls;
using Centello.App.ViewModels;
using Centello.Application;

namespace Centello.App.Pages;

public partial class AiInvitePage : ContentPage
{
    public static string Route => $"//{nameof(SettingsPage)}/{nameof(AiInvitePage)}";

    private readonly AiInviteViewModel _viewModel;
    private readonly AiAccess _aiAccess;
    private readonly AppEntryRouter _router;
    private readonly ISnackbar _snackbar;
    private CancellationTokenSource? _visit;
    private Window? _window;

    public AiInvitePage(AiInviteViewModel viewModel, AiAccess aiAccess, AppEntryRouter router, ISnackbar snackbar)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _aiAccess = aiAccess;
        _router = router;
        _snackbar = snackbar;
        BindingContext = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _visit = new CancellationTokenSource();
        _aiAccess.Changed += OnAiAccessChanged;
        _window = Window;
        if (_window is not null)
            _window.Activated += OnWindowActivated;

        _viewModel.UpdateAccess();
    }

    protected override void OnDisappearing()
    {
        _aiAccess.Changed -= OnAiAccessChanged;
        if (_window is not null)
            _window.Activated -= OnWindowActivated;

        _window = null;
        _visit?.Cancel();
        _visit?.Dispose();
        _visit = null;
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed()
    {
        if (!SoftKeyboard.Dismiss(this))
            _ = Shell.Current.GoToAsync("..");

        return true;
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");

    private void OnWindowActivated(object? sender, EventArgs e) => _viewModel.UpdateAccess();

    private void OnAiAccessChanged() => Dispatcher.Dispatch(() =>
    {
        if (_visit is not null)
            _viewModel.UpdateAccess();
    });

    private async void OnShareClicked(object? sender, EventArgs e)
    {
        try
        {
            await Share.Default.RequestAsync(new ShareTextRequest { Title = "ID Centello", Text = _viewModel.InstallationId });
        }
        catch (Exception exception)
        {
            Debug.WriteLine(exception);
            _snackbar.Show("Non è stato possibile condividere l’ID");
        }
    }

    private async void OnActivateClicked(object? sender, EventArgs e)
    {
        SoftKeyboard.Dismiss(this);
        await WhileVisible(async cancellationToken =>
        {
            await _viewModel.Activate(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;

            if (_viewModel.IsActive)
                SemanticScreenReader.Default.Announce("Invito attivo");
            else if (_viewModel.HasError)
                SemanticScreenReader.Default.Announce(_viewModel.ErrorMessage);
        });
    }

    private async void OnManualClicked(object? sender, EventArgs e) =>
        await Shell.Current.GoToAsync(ExpenseEditorPage.Route());

    private async void OnTodayClicked(object? sender, EventArgs e)
    {
        _router.Receive(new AppEntry.Screen.Today());
        await _router.OpenWaiting();
    }

    private async Task WhileVisible(Func<CancellationToken, Task> action)
    {
        if (_visit is null)
            return;

        var token = _visit.Token;
        try
        {
            await action(token);
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
    }
}
