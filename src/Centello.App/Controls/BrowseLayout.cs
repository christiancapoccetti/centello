namespace Centello.App.Controls;

internal static class BrowseLayout
{
    public static PhoneLandscapeTrigger FindTrigger(VisualElement layout) =>
        VisualStateManager.GetVisualStateGroups(layout)
            .SelectMany(group => group.States)
            .SelectMany(state => state.StateTriggers)
            .OfType<PhoneLandscapeTrigger>()
            .Single();
}