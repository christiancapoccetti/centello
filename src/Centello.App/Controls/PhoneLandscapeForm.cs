using Centello.App.Theming;
using Centello.Application;

namespace Centello.App.Controls;

public sealed class PhoneLandscapeForm : Grid
{
    public static readonly BindableProperty IsSplitProperty = BindableProperty.Create(
        nameof(IsSplit), typeof(bool), typeof(PhoneLandscapeForm), false,
        propertyChanged: (view, _, _) => ((PhoneLandscapeForm)view).ArrangePanes());

    private readonly ContentView _primaryContent = new();
    private readonly ContentView _primaryTail = new();
    private readonly ContentView _secondaryContent = new();
    private readonly ContentView _secondaryTail = new();
    private readonly ContentView _footer = new();
    private readonly VerticalStackLayout _primaryFields = new();
    private readonly VerticalStackLayout _secondaryFields = new();
    private readonly Grid _panes = new();
    private readonly BoxView _divider = new() { InputTransparent = true };
    private readonly ScrollView _primaryScroll = new() { SafeAreaEdges = SafeAreaEdges.None };
    private readonly ScrollView _secondaryScroll = new() { SafeAreaEdges = SafeAreaEdges.None };
    private readonly ScrollView _viewport = new() { SafeAreaEdges = SafeAreaEdges.None };
    private ScrollView? _lastLandscapeScroll;
    private FormPosition? _positionToRestore;
    private bool _revealQueued;
    private double _portraitBottomPadding;

    public PhoneLandscapeForm()
    {
        _primaryFields.Children.Add(_primaryContent);
        _primaryFields.Children.Add(_primaryTail);
        _secondaryFields.Children.Add(_secondaryContent);
        _secondaryFields.Children.Add(_secondaryTail);
        _primaryScroll.Content = _primaryFields;
        _secondaryScroll.Content = _secondaryFields;
        _divider.SetThemeColor(BoxView.ColorProperty, ThemeColor.Hairline);

        _panes.Children.Add(_primaryScroll);
        _panes.Children.Add(_secondaryScroll);
        _panes.Children.Add(_divider);
        _viewport.Content = _panes;

        RowDefinitions = [new RowDefinition(GridLength.Star), new RowDefinition(GridLength.Auto)];
        this.Add(_viewport);
        this.Add(_footer, 0, 1);
        _viewport.SizeChanged += OnViewportSizeChanged;
        _primaryScroll.Scrolled += (_, _) => RecordLandscapeScroll(_primaryScroll);
        _secondaryScroll.Scrolled += (_, _) => RecordLandscapeScroll(_secondaryScroll);
        _footer.SizeChanged += (_, _) => UpdatePrimaryInsets();
        PortraitBottomPadding = 24;
        ArrangePanes();
    }

    public double PortraitBottomPadding
    {
        get => _portraitBottomPadding;
        set
        {
            _portraitBottomPadding = value;
            UpdateContentPadding();
        }
    }

    public bool IsSplit { get => (bool)GetValue(IsSplitProperty); set => SetValue(IsSplitProperty, value); }

    public View? Primary { get => _primaryContent.Content; set => _primaryContent.Content = value; }

    public View? PrimaryTail
    {
        get => _primaryTail.Content;
        set
        {
            _primaryTail.Content = value;
            PlaceSecondaryContent();
        }
    }

    public View? Secondary { get => _secondaryContent.Content; set => _secondaryContent.Content = value; }
    public View? SecondaryTail { get => _secondaryTail.Content; set => _secondaryTail.Content = value; }
    public View? Footer { get => _footer.Content; set => _footer.Content = value; }

    public ScrollView ScrollFor(Element element)
    {
        if (!IsSplit)
            return _viewport;

        for (var parent = element.Parent; parent is not null; parent = parent.Parent)
        {
            if (parent == _primaryScroll)
                return _primaryScroll;

            if (parent == _secondaryScroll)
                return _secondaryScroll;
        }

        return _primaryScroll;
    }

    public async Task ShowFirstError()
    {
        var first = _viewport.GetVisualTreeDescendants().OfType<InlineError>()
            .FirstOrDefault(error => error.IsVisible);
        if (first is not null)
            await ScrollFor(first).ScrollToAsync(first, ScrollToPosition.Center, true);
    }

    private void ArrangePanes()
    {
        var split = IsSplit;
        var position = CaptureFormPosition();
        _positionToRestore = position;

        ColumnDefinitions = CreateColumns(split);
        _panes.ColumnDefinitions = CreateColumns(split);
        _panes.RowDefinitions = split
            ? [new RowDefinition(GridLength.Star)]
            : [new RowDefinition(GridLength.Auto), new RowDefinition(GridLength.Auto)];

        Grid.SetColumnSpan(_viewport, split ? 3 : 1);
        Grid.SetRowSpan(_viewport, split ? 2 : 1);
        Grid.SetRow(_secondaryScroll, split ? 0 : 1);
        Grid.SetColumn(_secondaryScroll, split ? 2 : 0);
        Grid.SetColumn(_divider, split ? 1 : 0);
        _divider.IsVisible = split;

        // Keeping the scroll hosts mounted leaves text entry and the keyboard on the same native controls.
        _viewport.Orientation = split ? ScrollOrientation.Neither : ScrollOrientation.Vertical;
        _primaryScroll.Orientation = split ? ScrollOrientation.Vertical : ScrollOrientation.Neither;
        _secondaryScroll.Orientation = _primaryScroll.Orientation;
        _primaryScroll.VerticalOptions = split ? LayoutOptions.Fill : LayoutOptions.Start;
        _secondaryScroll.VerticalOptions = _primaryScroll.VerticalOptions;
        PlaceSecondaryContent();
        UpdateContentPadding();
        UpdateViewport();
        if (position is not null)
            RestoreAfterLayout(position);
    }

    private FormPosition? CaptureFormPosition()
    {
        if (!IsLoaded)
            return null;

        if (_positionToRestore is not null)
            return _positionToRestore with { };

        var oldScroll = _viewport.Orientation == ScrollOrientation.Vertical
            ? _viewport
            : _lastLandscapeScroll ?? _primaryScroll;
        var scroll = CaptureScroll(oldScroll);
        var input = FocusedInput() is { } field
            ? new InputPosition(field, field.CursorPosition, field.SelectionLength, field.IsSoftInputShowing())
            : null;
        return new FormPosition(scroll, input);
    }

    private static ScrollPosition? CaptureScroll(ScrollView scroll)
    {
        var top = scroll.ScrollY;
        var bottom = top + scroll.Height;
        var candidate = scroll.GetVisualTreeDescendants().OfType<VisualElement>()
            .Where(view => view is Label or InputView or Button or Switch or DatePicker)
            .Where(view => IsArrangedIn(view, scroll))
            .Select(view => (View: view, Y: scroll.GetScrollPositionForElement(view, ScrollToPosition.Start).Y))
            .Where(item => item.Y + item.View.Height > top && item.Y < bottom)
            .OrderBy(item => Math.Max(0, item.Y - top))
            .FirstOrDefault();
        return candidate.View is null ? null : new ScrollPosition(candidate.View, top - candidate.Y);
    }

    private static bool IsArrangedIn(VisualElement view, ScrollView scroll)
    {
        if (view.Height <= 0 || view.Width <= 0)
            return false;

        for (Element? parent = view; parent is not null; parent = parent.Parent)
        {
            if (parent == scroll)
                return true;

            if (parent is VisualElement { IsVisible: false })
                return false;
        }

        return false;
    }

    private void RecordLandscapeScroll(ScrollView scroll)
    {
        if (IsSplit && _positionToRestore is null)
            _lastLandscapeScroll = scroll;
    }

    private void RestoreAfterLayout(FormPosition position)
    {
#if ANDROID
        if (Handler?.PlatformView is Android.Views.View view && view.ViewTreeObserver is { IsAlive: true } observer)
        {
            // The new pane widths can wrap the same element differently. Wait for its new coordinates.
            observer.GlobalLayout += OnLayout;
            Unloaded += OnUnloaded;
            view.RequestLayout();
            return;

            void OnLayout(object? sender, EventArgs e)
            {
                StopWaiting();
                view.Post(() => _ = RestorePosition(position));
            }

            void OnUnloaded(object? sender, EventArgs e)
            {
                StopWaiting();
                if (ReferenceEquals(_positionToRestore, position))
                    _positionToRestore = null;
            }

            void StopWaiting()
            {
                if (observer.IsAlive)
                    observer.GlobalLayout -= OnLayout;

                Unloaded -= OnUnloaded;
            }
        }
#endif
        Dispatcher.Dispatch(() => _ = RestorePosition(position));
    }

    private async Task RestorePosition(FormPosition position)
    {
        if (!ReferenceEquals(_positionToRestore, position) || !IsLoaded)
            return;

        try
        {
            if (position.Scroll is { } anchor)
            {
                var scroll = ScrollFor(anchor.Element);
                if (IsArrangedIn(anchor.Element, scroll))
                {
                    var start = scroll.GetScrollPositionForElement(anchor.Element, ScrollToPosition.Start).Y;
                    var offset = Math.Min(anchor.Offset, anchor.Element.Height - 1);
                    await scroll.ScrollToAsync(0, Math.Max(0, start + offset), false);
                    if (IsSplit)
                        _lastLandscapeScroll = scroll;
                }
            }

            if (ReferenceEquals(_positionToRestore, position) && position.Input is { } input)
                await RestoreInput(input);
        }
        finally
        {
            if (ReferenceEquals(_positionToRestore, position))
                _positionToRestore = null;
        }
    }

    private async Task RestoreInput(InputPosition position)
    {
        var field = position.Field;
        var scroll = ScrollFor(field);
        if (!IsArrangedIn(field, scroll) || FocusedInput() is { } focused && focused != field)
            return;

        if (!field.IsFocused)
            field.Focus();

        var textLength = field.Text?.Length ?? 0;
        field.CursorPosition = Math.Clamp(position.Cursor, 0, textLength);
        field.SelectionLength = Math.Clamp(position.Selection, 0, textLength - field.CursorPosition);
        await scroll.ScrollToAsync(field, ScrollToPosition.MakeVisible, false);
        if (position.KeyboardVisible && field.IsFocused && !field.IsSoftInputShowing())
            _ = field.ShowSoftInputAsync(CancellationToken.None);
    }

    private InputView? FocusedInput() =>
        _viewport.GetVisualTreeDescendants().OfType<InputView>()
            .FirstOrDefault(view => view.IsFocused);

    private sealed record ScrollPosition(VisualElement Element, double Offset);
    private sealed record InputPosition(InputView Field, int Cursor, int Selection, bool KeyboardVisible);
    private sealed record FormPosition(ScrollPosition? Scroll, InputPosition? Input);

    private static ColumnDefinitionCollection CreateColumns(bool split) => split
        ? [new ColumnDefinition(new GridLength(.92, GridUnitType.Star)), new ColumnDefinition(new GridLength(1)),
            new ColumnDefinition(new GridLength(1.08, GridUnitType.Star))]
        : [new ColumnDefinition(GridLength.Star)];

    private void PlaceSecondaryContent()
    {
        // The selectors fall between the text fields in portrait. Move just the selectors, keeping both fields mounted.
        var host = IsSplit || PrimaryTail is null ? _secondaryFields : _primaryFields;
        if (_secondaryContent.Parent == host)
            return;

        if (_secondaryContent.Parent is Layout previous)
            previous.Children.Remove(_secondaryContent);

        host.Children.Insert(host == _primaryFields ? 1 : 0, _secondaryContent);
    }

    private void OnViewportSizeChanged(object? sender, EventArgs e) => UpdateViewport();

    private void UpdateViewport()
    {
        // Android can measure a ScrollView child without a height constraint even when scrolling is disabled.
        _panes.HeightRequest = IsSplit && _viewport.Height > 0 ? _viewport.Height : -1;
        UpdatePrimaryInsets();

        if (_revealQueued || !IsLoaded || _positionToRestore is not null)
            return;

        _revealQueued = true;
        Dispatcher.Dispatch(() =>
        {
            _revealQueued = false;
            if (_positionToRestore is null && FocusedInput() is { } input)
                _ = ScrollFor(input).ScrollToAsync(input, ScrollToPosition.MakeVisible, false);
        });
    }

    private void UpdateContentPadding()
    {
        _primaryFields.Padding = new Thickness(0, 0, 0, IsSplit ? 16 : 0);
        _secondaryFields.Padding = new Thickness(0, 0, 0, IsSplit ? 16 : PortraitBottomPadding);
    }

    private void UpdatePrimaryInsets() =>
        _primaryScroll.Margin = new Thickness(0, 0, 0, IsSplit ? Math.Max(0, _footer.Height) : 0);
}
