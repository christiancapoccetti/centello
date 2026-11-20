using Android.Views;
using Android.Widget;

namespace Centesimo.App;

/// <summary>A place in a widget layout holding a light and a dark copy of the same thing. The copies take their
/// visibility from the Widget.Centesimo.Day and .Night styles, which values-night swaps, so code never sets it: a copy
/// whose visibility came from code would stop following the scheme. Hiding the place for lack of data acts on the
/// container.</summary>
internal readonly record struct DayNightSlot(int Container, int Day, int Night)
{
    public void Show(RemoteViews views, DayNight art)
    {
        views.SetImageViewBitmap(Day, art.Day);
        views.SetImageViewBitmap(Night, art.Night);
        views.SetViewVisibility(Container, ViewStates.Visible);
    }

    public void Hide(RemoteViews views, ViewStates how = ViewStates.Gone) => views.SetViewVisibility(Container, how);
}
