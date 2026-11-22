using Android.Content;
using AccessibilityManager = Android.Views.Accessibility.AccessibilityManager;

namespace Centello.App;

public sealed class AndroidScreenReader : IScreenReader
{
    private readonly AccessibilityManager? _manager = Microsoft.Maui.ApplicationModel.Platform.AppContext
        .GetSystemService(Context.AccessibilityService) as AccessibilityManager;

    public AndroidScreenReader()
    {
        if (_manager is { } manager)
            manager.AddTouchExplorationStateChangeListener(new TouchExplorationListener(() => Changed?.Invoke(this, EventArgs.Empty)));
    }

    public bool IsOn => _manager is { IsTouchExplorationEnabled: true };
    public event EventHandler? Changed;

    private sealed class TouchExplorationListener(Action changed) : Java.Lang.Object,
        AccessibilityManager.ITouchExplorationStateChangeListener
    {
        public void OnTouchExplorationStateChanged(bool enabled) => changed();
    }
}
