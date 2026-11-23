using Centello.App.Controls;
using Centello.App.Controls.Recap;
using Centello.App.Theming;
using Centello.Application;
using Centello.Domain;

namespace Centello.App.Pages;

internal sealed class SpendingMapSharePage : ContentPage
{
    private readonly RecapCard.SpendingMap _map;
    private readonly bool _isDark;
    private readonly SpendingMapFiles _files;
    private readonly Segmented _scope = new() { Items = ["Storia", "Mappa completa"], HeightRequest = 48 };
    private readonly Segmented _values = new() { Items = ["Solo percentuali", "Con importi"], HeightRequest = 48 };
    private readonly Image _preview = new() { Aspect = Aspect.AspectFit };
    private readonly Button _share;
    private readonly Button _save;
    private readonly Button _close;
    private readonly Label _status;
    private readonly Grid _body = new() { RowSpacing = 12 };
    private readonly ScrollView _settings;
    private bool _visible;
    private bool _rendering;
    private bool _exporting;
    private bool _wide;
    private int _revision;
    private byte[]? _png;

    public SpendingMapSharePage(RecapCard.SpendingMap map, bool complete, bool isDark, IDownloads downloads)
    {
        _map = map;
        _isDark = isDark;
        _files = new SpendingMapFiles(downloads);
        _scope.SelectedIndex = complete ? 1 : 0;
        var ink = StoryInk.Of(RecapSurface.Ground, isDark);
        Title = "Anteprima immagine";
        BackgroundColor = ink.Ground;
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container);
        var heading = StoryBlocks.Headline(Title, ink, 22);
        heading.VerticalOptions = LayoutOptions.Center;
        SemanticProperties.SetHeadingLevel(heading, SemanticHeadingLevel.Level1);
        _close = StoryBlocks.Pill("Chiudi", false, ink, Close);
        var header = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions = [new(GridLength.Star), new(GridLength.Auto)]
        };
        header.Add(heading);
        header.Add(_close, 1);

        _share = StoryBlocks.Pill("Condividi", true, ink, () => Export(true));
        _save = StoryBlocks.Pill("Salva immagine", false, ink, () => Export(false));
        _status = StoryBlocks.Caption("", ink, 14);
        _status.IsVisible = false;
        var actions = new Grid
        {
            ColumnSpacing = 8,
            ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star)]
        };
        _save.Padding = _share.Padding = new Thickness(8, 0);
        actions.Add(_save);
        actions.Add(_share, 1);
        _settings = new ScrollView
        {
            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children = { _scope, _values, actions, _status }
            }
        };
        _body.RowDefinitions = [new(GridLength.Star), new(GridLength.Auto)];
        _body.Add(_preview);
        _body.Add(_settings, 0, 1);
        var page = new Grid
        {
            Padding = new Thickness(16, 8), RowSpacing = 10,
            RowDefinitions = [new(GridLength.Auto), new(GridLength.Star)]
        };
        page.Add(header);
        page.Add(_body, 0, 1);
        Content = page;
        SizeChanged += (_, _) => ArrangePreview();
        _scope.PropertyChanged += OnSelectionChanged;
        _values.PropertyChanged += OnSelectionChanged;
        EnableActions();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _visible = true;
        RefreshPreview();
    }

    protected override void OnDisappearing()
    {
        _visible = false;
        _revision++;
        base.OnDisappearing();
    }

    protected override bool OnBackButtonPressed() => _exporting || base.OnBackButtonPressed();

    private void ArrangePreview()
    {
        var wide = Width > Height && Width >= 520;
        if (wide == _wide)
            return;

        _wide = wide;
        _body.RowDefinitions = wide ? [new(GridLength.Star)] : [new(GridLength.Star), new(GridLength.Auto)];
        _body.ColumnDefinitions = wide ? [new(GridLength.Star), new(new GridLength(0.85, GridUnitType.Star))] : [new(GridLength.Star)];
        _body.ColumnSpacing = wide ? 16 : 0;
        Grid.SetRow(_settings, wide ? 0 : 1);
        Grid.SetColumn(_settings, wide ? 1 : 0);
    }

    private void OnSelectionChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs args)
    {
        if (args.PropertyName == nameof(Segmented.SelectedIndex))
            RefreshPreview();
    }

    // Only the latest requested image can become actionable. Rapid switches share one render loop.
    private async void RefreshPreview()
    {
        _revision++;
        _png = null;
        _preview.Source = null;
        SemanticProperties.SetDescription(_preview, "");
        EnableActions();
        if (!_visible || _rendering)
            return;

        _rendering = true;
        try
        {
            while (_visible)
            {
                var revision = _revision;
                var complete = _scope.SelectedIndex == 1;
                var amounts = _values.SelectedIndex == 1;
                Status("Preparazione dell’immagine…");
                byte[] png;
                string description;
                try
                {
                    (png, description) = await Task.Run(() =>
                    {
                        var art = new SpendingMapArt(_map, complete, amounts, _isDark);
                        return (SpendingMapPng.Render(art), art.Describe());
                    });
                }
                catch (Exception)
                {
                    if (revision != _revision)
                        continue;

                    Status("Non è stato possibile creare l’immagine. Prova un altro formato.");
                    return;
                }

                if (revision != _revision)
                    continue;

                _preview.Source = ImageSource.FromStream(() => new MemoryStream(png, writable: false));
                SemanticProperties.SetDescription(_preview, description);
                _png = png;
                Status("");
                return;
            }
        }
        finally
        {
            _rendering = false;
            EnableActions();
        }
    }

    private async void Export(bool share)
    {
        if (_exporting || _png is not { } png)
            return;

        _exporting = true;
        EnableActions();
        try
        {
            var period = _map.Span.Period;
            var date = period.Start.ToString(period.Kind == PeriodKind.Month ? "yyyy-MM" : "yyyy", System.Globalization.CultureInfo.InvariantCulture);
            var scope = _scope.SelectedIndex == 1 ? "mappa" : "storia";
            var result = await _files.Write(png, $"centello-{scope}-{date}.png", share);
            Status(result.IsSuccess ? result.Value : share ? "Non è stato possibile condividere l’immagine." : "Non è stato possibile salvare l’immagine.");
        }
        finally
        {
            _exporting = false;
            EnableActions();
        }
    }

    private void EnableActions()
    {
        _share.IsEnabled = _save.IsEnabled = !_exporting && _png is not null;
        _scope.IsEnabled = _values.IsEnabled = _close.IsEnabled = !_exporting;
    }

    private void Status(string text)
    {
        _status.Text = text;
        _status.IsVisible = text.HasValue();
    }

    private async void Close()
    {
        if (!_exporting && _visible && Navigation.ModalStack.LastOrDefault() == this)
        {
            _visible = false;
            await Navigation.PopModalAsync();
        }
    }
}
