using Centesimo.App.Controls.Recap;
using Centesimo.App.Theming;
using Centesimo.App.ViewModels;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.Pages;

/// <summary>The story of a month or a year, full screen over Insight: one card at a time, a tap on the right half
/// goes on and one on the left half goes back. The page paints edge to edge, so each card draws its own surface and
/// the system bars follow the card in front.</summary>
public partial class RecapPage : ContentPage, IQueryAttributable
{
    private readonly RecapViewModel _viewModel;
    private readonly IStoryBars _bars;
    private readonly StoryTouches _touches;
    private readonly StoryPlayer _player;
    private readonly RecapPress _press = new();
    private Window? _window;
    private bool _isInitialized;
    private double _builtWidth;

    public RecapPage(RecapViewModel viewModel, IStoryBars bars, StoryTouches touches, IScreenReader screenReader)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _bars = bars;
        _touches = touches;
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

        _touches.Pressed += OnPressed;
        _touches.Released += OnReleased;
        if (Window is { } window)
        {
            _window = window;
            window.Activated += OnWindowActivated;
            window.Deactivated += OnWindowDeactivated;
        }

        _player.IsActive = true;
        _player.IsVisible = true;
        await _viewModel.Load();
        Show();
        if (_viewModel.Story is { } story)
            _player.Play(story);
    }

    // The bars belong to the card in front: another page over the story gives them back. The clock waits with them.
    protected override void OnDisappearing()
    {
        if (Microsoft.Maui.Controls.Application.Current is { } app)
            app.RequestedThemeChanged -= OnThemeChanged;

        _touches.Pressed -= OnPressed;
        _touches.Released -= OnReleased;
        if (_window is { } window)
        {
            window.Activated -= OnWindowActivated;
            window.Deactivated -= OnWindowDeactivated;
            _window = null;
        }

        _press.Up(Now);
        _player.IsHeld = false;
        _player.IsVisible = false;
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

    private void Rebuild()
    {
        if (Math.Abs(Width - _builtWidth) > 0.5)
            Show();
    }

    private void Show()
    {
        if (_viewModel.Story is not { } story || Width <= 0)
            return;

        _builtWidth = Width;
        var card = story.Current;
        var ink = StoryInk.Of(card.Top, IsDark);
        CardHost.Content = card.Accept(new RecapCardViews(IsDark, Width, OpenExpense, Exit, Restart));
        Progress.Show(story.Index, story.Count, ink, _player.Fill);
        TitleLabel.TextColor = ink.Ink;
        TitleLabel.FormattedText = TitleOf(story.Deck, ink);
        CloseButton.TextColor = ink.Ink;
        _bars.Hold(card.Top, card.Bottom);
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
        if (_press.WasHold || e.GetPosition(CardHost) is not { } at)
            return;

        var moved = at.X < CardHost.Width / 2 ? _viewModel.Previous() : _viewModel.Next();
        if (moved)
            Show();
        else
            Progress.Fill(_player.Fill);

        _player.Refresh();
    }

    private void Restart()
    {
        _viewModel.Restart();
        Show();
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

    private async void OnCloseClicked(object? sender, EventArgs e) => await Shell.Current.GoToAsync("..");
}
