using AndroidView = Android.Views.View;
using AndroidViewTreeObserver = Android.Views.ViewTreeObserver;

namespace Centello.App.Controls;

internal sealed class BrowseScrollAnchor
{
    private readonly ScrollView _primary;
    private readonly ScrollView _secondary;
    private ScrollView _lastScrolled;
    private Anchor? _pending;
    private AndroidViewTreeObserver? _observer;
    private bool _restoring;

    public BrowseScrollAnchor(ScrollView primary, ScrollView secondary)
    {
        _primary = primary;
        _secondary = secondary;
        _lastScrolled = primary;
        _primary.Scrolled += OnScrolled;
        _secondary.Scrolled += OnScrolled;
        _primary.Unloaded += OnUnloaded;
    }

    public void CaptureBeforeReflow()
    {
        if (_pending is not null)
            return;

        var source = _secondary.IsVisible ? _lastScrolled : _primary;
        if (source.Content is not View content || source.Height <= 0)
            return;

        _pending = source == _primary && source.ScrollY <= 0
            ? new Anchor(null, default)
            : FirstVisible(content, 0, source.ScrollY);
    }

    public void RestoreAfterLayout()
    {
        if (_pending is null)
            return;

        if (_primary.Handler?.PlatformView is not AndroidView native ||
            native.ViewTreeObserver is not { IsAlive: true } observer)
        {
            _pending = null;
            return;
        }

        // A native layout pass updates both the viewport height and the anchor's new position.
        // The breakpoint itself can fire while a view still reports its old bounds.
        StopObserving();
        _observer = observer;
        observer.GlobalLayout += OnLayout;
    }

    private void OnLayout(object? sender, EventArgs args)
    {
        StopObserving();

        if (_pending is not { } anchor)
            return;

        _pending = null;
        var destination = _primary;
        var offset = 0d;
        var view = anchor.View;
        if (view is not null && Locate(view) is { } location)
        {
            destination = location.Scroll;
            offset = anchor.Offset.Restore(location.Top, view.Height, destination.ContentSize.Height, destination.Height);
        }

        _restoring = true;
        _lastScrolled = destination;
        _ = destination.ScrollToAsync(0, offset, false);
        _restoring = false;
    }

    private void OnUnloaded(object? sender, EventArgs args)
    {
        StopObserving();
        _pending = null;
    }

    private void StopObserving()
    {
        if (_observer is { IsAlive: true } observer)
            observer.GlobalLayout -= OnLayout;

        _observer = null;
    }

    private void OnScrolled(object? sender, ScrolledEventArgs args)
    {
        if (!_restoring && _pending is null && sender is ScrollView { IsVisible: true } scroll)
            _lastScrolled = scroll;
    }

    private (ScrollView Scroll, double Top)? Locate(View view)
    {
        var top = view.Y;
        for (var parent = view.Parent; parent is VisualElement element; parent = parent.Parent)
        {
            if (element == _primary)
                return (_primary, top);

            if (element == _secondary)
                return (_secondary, top);

            top += element.Y;
        }

        return null;
    }

    private static Anchor FirstVisible(View view, double parentTop, double offset)
    {
        var top = parentTop + view.Y;
        if (view is VerticalStackLayout stack)
        {
            foreach (var element in stack.Children)
            {
                if (element is View { IsVisible: true, Height: > 0 } child &&
                    top + child.Y + child.Height > offset)
                    return FirstVisible(child, top, offset);
            }
        }

        return new Anchor(view, BrowseScrollOffset.Capture(offset, top));
    }

    private readonly record struct Anchor(View? View, BrowseScrollOffset Offset);
}
