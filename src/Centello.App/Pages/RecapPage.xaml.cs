using Centello.App.Controls;
using Centello.App.Controls.Recap;
using Centello.App.Theming;
using Centello.App.ViewModels;
using Centello.Application;
using Centello.Domain;

namespace Centello.App.Pages;

/// <summary>The story of a month or year, full screen over Insight. It plays automatically with tap navigation or
/// uses explicit controls with a screen reader. Each card draws its own surface, including the system bars.</summary>
public partial class RecapPage : ContentPage, IQueryAttributable
{
    private readonly RecapViewModel _viewModel;
    private readonly IStoryBars _bars;
    private readonly StoryTouches _touches;
    private readonly StoryPlayer _player;
    private readonly IScreenReader _screenReader;
    private readonly IDownloads _downloads;
    private readonly RecapPress _press = new();
    private Window? _window;
    private bool _isInitialized;
    private bool _isShowing;
    private bool _focusPending;
    private bool _openingPreview;
    private double _builtWidth;
    private double _builtHeight;
    private bool _builtLandscape;

    public RecapPage(RecapViewModel viewModel, IStoryBars bars, StoryTouches touches, IScreenReader screenReader, IDownloads downloads)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _bars = bars;
        _touches = touches;
        _screenReader = screenReader;
        _downloads = downloads;
        _player = new StoryPlayer(Dispatcher.CreateTimer(), screenReader);
        _player.CardChanged += Show;
        _player.Progressed += Progress.Fill;
        BindingContext = viewModel;
        SizeChanged += (_, _) => Rebuild();
    }

    /// <summary>The route that opens the story of a month or of a year.</summary>
    public static string Route(Period period) =>
        $"{nameof(RecapPage)}?year={period.Start.Year}" + (period.Kind == PeriodKind.Month ? $"&month={period.Start.Month}" : "");

    private static bool IsDark => Microsoft.Maui.Controls.Application.Current?.RequestedTheme == AppTheme.Dark;

    // Shell applies the query again when the page comes back to the front. Only the first one opens the story.
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (_isInitialized || !query.TryGetValue("year", out var yearValue) || !int.TryParse(yearValue?.ToString(), out var year))
            return;

        _isInitialized = true;
        _viewModel.Initialize(query.TryGetValue("month", out var monthValue) && int.TryParse(monthValue?.ToString(), out var month)
            ? Period.Month(year, month)
            : Period.Year(year));
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (Microsoft.Maui.Controls.Application.Current is { } app)
            app.RequestedThemeChanged += OnThemeChanged;

        _screenReader.Changed += OnScreenReaderChanged;
        _touches.Pressed += OnPressed;
        _touches.Released += OnReleased;
        if (Window is { } window)
        {
            _window = window;
            window.Activated += OnWindowActivated;
            window.Deactivated += OnWindowDeactivated;
        }

        _isShowing = true;
        UpdateNavigation(PhoneLandscape.IsActive(this));
        _player.IsActive = true;
        _player.IsVisible = true;
        await _viewModel.Load();
        if (!_isShowing)
            return;

        if (_viewModel.Story is { } story)
            _player.Play(story);

        ShowAndFocus();
    }

    // The bars belong to the card in front: another page over the story gives them back. The clock waits with them.
    protected override void OnDisappearing()
    {
        if (Microsoft.Maui.Controls.Application.Current is { } app)
            app.RequestedThemeChanged -= OnThemeChanged;

        _screenReader.Changed -= OnScreenReaderChanged;
        _touches.Pressed -= OnPressed;
        _touches.Released -= OnReleased;
        if (_window is { } window)
        {
            window.Activated -= OnWindowActivated;
            window.Deactivated -= OnWindowDeactivated;
            _window = null;
        }

        _isShowing = false;
        _press.Up(Now);
        _player.IsVisible = false;
        _player.IsHeld = false;
        _bars.Release();
        base.OnDisappearing();
    }

    private static TimeSpan Now => TimeSpan.FromMilliseconds(Environment.TickCount64);

    // The story stops the moment a finger lands and goes on when it lifts.
    private void OnPressed()
    {
        _press.Down(Now);
        _player.IsHeld = true;
    }

    private void OnReleased()
    {
        _press.Up(Now);
        _player.IsHeld = false;
    }

    private void OnWindowActivated(object? sender, EventArgs e) => _player.IsActive = true;

    private void OnWindowDeactivated(object? sender, EventArgs e) => _player.IsActive = false;

    private void OnThemeChanged(object? sender, AppThemeChangedEventArgs e) => Show();

    private void OnScreenReaderChanged(object? sender, EventArgs e)
    {
        UpdateNavigation(PhoneLandscape.IsActive(this));
        _player.Refresh();
        ShowAndFocus();
    }

    private void UpdateNavigation(bool landscape)
    {
        var usesScreenReader = !_player.IsAutomatic;
        var manualCard = _viewModel.Story?.WaitsForInput == true;
        NavigationButtons.IsVisible = landscape || usesScreenReader || manualCard;
        if (usesScreenReader)
            CardHost.GestureRecognizers.Remove(CardTap);
        else if (!CardHost.GestureRecognizers.Contains(CardTap))
            CardHost.GestureRecognizers.Add(CardTap);
    }

    private void Rebuild()
    {
        var landscape = PhoneLandscape.IsActive(this);
        if (_builtLandscape != landscape || Math.Abs(Width - _builtWidth) > 0.5 ||
            (landscape && Math.Abs(Height - _builtHeight) > 0.5))
            Show();
    }

    private void ShowAndFocus()
    {
        _focusPending = !_player.IsAutomatic;
        Show();
    }

    private void Show()
    {
        if (!_isShowing || _viewModel.Story is not { } story || Width <= 0)
            return;

        _builtWidth = Width;
        _builtHeight = Height;
        _builtLandscape = PhoneLandscape.IsActive(this);
        UpdateNavigation(_builtLandscape);
        var card = story.Current;
        var ink = StoryInk.Of(card.Top, IsDark);
        CardHost.Content = card.Accept(new RecapCardViews(IsDark, Width, Height, _builtLandscape, OpenExpense, Exit, Restart, ShareMap));
        if (_player.IsAutomatic && CardHost.Content is SpendingMapView map)
            map.EnableTapNavigation(OnCardTapped);
        var index = story.Index;
        Progress.Show(index, story.Count, ink, _player.Fill);
        TitleLabel.TextColor = ink.Ink;
        TitleLabel.FormattedText = TitleOf(story.Deck, ink);
        SemanticProperties.SetDescription(TitleLabel, $"{TitleLabel.FormattedText}. Scheda {index + 1} di {story.Count}");
        PreviousButton.TextColor = ink.Ink;
        PreviousButton.IsEnabled = index > 0;
        NextButton.TextColor = ink.Ink;
        NextButton.IsEnabled = index < story.Count - 1;
        CloseButton.TextColor = ink.Ink;
        _bars.Hold(card.Top, card.Bottom);

        if (!_focusPending)
            return;

        _focusPending = false;
        Dispatcher.Dispatch(() =>
        {
            if (_isShowing && !_player.IsAutomatic && story.Index == index)
                TitleLabel.SetSemanticFocus();
        });
    }

    private static FormattedString TitleOf(RecapDeck deck, StoryInk ink)
    {
        var title = new FormattedString { Spans = { new Span { Text = deck.Title } } };
        if (deck.Badge.HasValue())
            title.Spans.Add(new Span { Text = $" {deck.Badge}", FontFamily = Fonts.Regular, TextColor = ink.Mute });

        return title;
    }

    // A press that lasted was a hold, not a tap: the story only paused.
    private void OnCardTapped(object? sender, TappedEventArgs e)
    {
        if (!_player.IsAutomatic || _press.WasHold || e.GetPosition(CardHost) is not { } at)
            return;

        Move(previous: at.X < CardHost.Width / 2);
    }

    private void OnPreviousClicked(object? sender, EventArgs e) => Move(previous: true);

    private void OnNextClicked(object? sender, EventArgs e) => Move(previous: false);

    private void Move(bool previous)
    {
        var moved = previous ? _viewModel.Previous() : _viewModel.Next();
        if (moved)
            ShowAndFocus();
        else
            Progress.Fill(_player.Fill);

        _player.Refresh();
    }

    private void Restart()
    {
        _viewModel.Restart();
        ShowAndFocus();
        _player.Refresh();
    }

    private async void Exit(RecapExitTarget target)
    {
        if (target == RecapExitTarget.MonthExpenses && _viewModel.Period is { } month)
            await Shell.Current.GoToAsync(ExpensesPage.Route(month));
        else
            await Shell.Current.GoToAsync("..");
    }

    private async void OpenExpense(Guid expenseId) => await Shell.Current.GoToAsync(ExpenseEditorPage.Route(expenseId));

    private async void ShareMap(RecapCard.SpendingMap card, bool complete)
    {
        if (_openingPreview || !_isShowing)
            return;

        _openingPreview = true;
        try
        {
            await Navigation.PushModalAsync(new SpendingMapSharePage(card, complete, IsDark, _downloads));
        }
        finally
        {
            _openingPreview = false;
        }
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
