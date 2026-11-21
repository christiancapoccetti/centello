using Android.Content;
using Android.Views.Accessibility;

namespace Centesimo.App;

public sealed class AndroidScreenReader : IScreenReader
{
    public bool IsOn => Microsoft.Maui.ApplicationModel.Platform.AppContext.GetSystemService(Context.AccessibilityService) is AccessibilityManager { IsTouchExplorationEnabled: true };
}
