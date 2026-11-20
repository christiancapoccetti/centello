namespace Centesimo.App.Controls;

/// <summary>What Back does while the soft keyboard is open. Android hands Back to the app's own back callback ahead
/// of the keyboard on some versions, so a page that treats Back as "leave" would leave with the keyboard still up. The
/// pages with fields ask here first: the first Back only closes the keyboard, and the next one follows the page's own
/// path.</summary>
public static class SoftKeyboard
{
    /// <summary>Takes the focus off the field that holds the keyboard and returns true. Returns false when no keyboard
    /// is open, so the page goes on with its own Back.</summary>
    public static bool Dismiss(Page page)
    {
        var field = page
            .GetVisualTreeDescendants()
            .OfType<InputView>()
            .FirstOrDefault(input => input.IsFocused);
        if (field is null || !field.IsSoftInputShowing())
            return false;

        field.Unfocus();
        _ = field.HideSoftInputAsync(CancellationToken.None);
        return true;
    }
}
