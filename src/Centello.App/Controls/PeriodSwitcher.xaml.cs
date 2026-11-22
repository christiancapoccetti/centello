using Centello.Application;

namespace Centello.App.Controls;

/// <summary>The period a page shows and the way to another one: the month or the year with its arrows. A tap on the
/// title asks to pass between month and year, where the page can. Oggi, Insight, a category and the expense history
/// share it, so a period reads and moves the same everywhere.</summary>
public partial class PeriodSwitcher : ContentView
{
    public static readonly BindableProperty PeriodProperty = BindableProperty.Create(nameof(Period), typeof(Period),
        typeof(PeriodSwitcher), propertyChanged: (switcher, _, _) => ((PeriodSwitcher)switcher).Apply());
    public static readonly BindableProperty HasNextProperty = BindableProperty.Create(nameof(HasNext), typeof(bool),
        typeof(PeriodSwitcher), false);
    public static readonly BindableProperty CanToggleProperty = BindableProperty.Create(nameof(CanToggle), typeof(bool),
        typeof(PeriodSwitcher), true, propertyChanged: (switcher, _, _) => ((PeriodSwitcher)switcher).Apply());
    public static readonly BindableProperty IsPageTitleProperty = BindableProperty.Create(nameof(IsPageTitle), typeof(bool),
        typeof(PeriodSwitcher), false, propertyChanged: (switcher, _, _) => ((PeriodSwitcher)switcher).Apply());

    public PeriodSwitcher()
    {
        InitializeComponent();
        Apply();
    }

    public event EventHandler? PreviousTapped;
    public event EventHandler? NextTapped;
    public event EventHandler? ToggleTapped;

    public Period? Period { get => (Period?)GetValue(PeriodProperty); set => SetValue(PeriodProperty, value); }
    /// <summary>False on the period that contains today: nothing was spent after it.</summary>
    public bool HasNext { get => (bool)GetValue(HasNextProperty); set => SetValue(HasNextProperty, value); }
    /// <summary>False where the page shows months only: the title is then plain text.</summary>
    public bool CanToggle { get => (bool)GetValue(CanToggleProperty); set => SetValue(CanToggleProperty, value); }
    /// <summary>True where the period is the header of the page and is written as large as a page title.</summary>
    public bool IsPageTitle { get => (bool)GetValue(IsPageTitleProperty); set => SetValue(IsPageTitleProperty, value); }

    private void Apply()
    {
        Title.FontSize = IsPageTitle ? 20 : 18;
        Year.FontSize = IsPageTitle ? 16 : 15;
        Cue.Text = CanToggle ? MaterialSymbols.ExpandMore : "";
        PeriodName.Text = Period is null ? "" : ItalianFormat.PeriodName(Period);
        Year.Text = Period?.Start.Year.ToString() ?? "";
        SemanticProperties.SetDescription(Title, Period is null ? "" : ItalianFormat.PeriodTitle(Period));
        SemanticProperties.SetHint(Title, CanToggle ? "Tocca per passare tra mese e anno" : "");
        SemanticProperties.SetDescription(Previous, CanToggle ? "Periodo precedente" : "Mese precedente");
        SemanticProperties.SetDescription(Next, CanToggle ? "Periodo successivo" : "Mese successivo");
    }

    private void OnTitleTapped(object? sender, TappedEventArgs e)
    {
        if (CanToggle)
            ToggleTapped?.Invoke(this, EventArgs.Empty);
    }

    private void OnPreviousClicked(object? sender, EventArgs e) => PreviousTapped?.Invoke(this, EventArgs.Empty);

    private void OnNextClicked(object? sender, EventArgs e) => NextTapped?.Invoke(this, EventArgs.Empty);
}
