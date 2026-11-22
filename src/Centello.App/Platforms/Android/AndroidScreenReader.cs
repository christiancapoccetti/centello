using Android.Content;
using Android.Views.Accessibility;

namespace Centello.App;

public sealed class AndroidScreenReader : IScreenReader
{
    private readonly AccessibilityManager? _manager = Microsoft.Maui.ApplicationModel.Platform.AppContext
        .GetSystemService(Context.AccessibilityService) as AccessibilityManager;

    public AndroidScreenReader()
    {
        if (_manager is { } manager)
            manager.TouchExplorationStateChange += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
    }

    public bool IsOn => _manager is { IsTouchExplorationEnabled: true };
    public event EventHandler? Changed;
}
