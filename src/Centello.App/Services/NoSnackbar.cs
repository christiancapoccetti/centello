namespace Centello.App;

public interface ISnackbar
{
    /// <summary>Says in one line what just happened, on the page now in front, then goes away by itself.</summary>
    void Show(string message);
}

/// <summary>Marks the view a snackbar sits above, so it never covers the controls docked at the bottom of a page.</summary>
public static class SnackbarAnchor
{
    public static readonly BindableProperty IsAnchorProperty = BindableProperty.CreateAttached("IsAnchor", typeof(bool),
        typeof(SnackbarAnchor), false);

    public static bool GetIsAnchor(BindableObject view) => (bool)view.GetValue(IsAnchorProperty);

    public static void SetIsAnchor(BindableObject view, bool value) => view.SetValue(IsAnchorProperty, value);
}

/// <summary>For platforms without a snackbar.</summary>
public sealed class NoSnackbar : ISnackbar
{
    public void Show(string message)
    {
    }
}
