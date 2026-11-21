using Android.Content;
using AndroidX.Core.Content.PM;
using AndroidX.Core.Graphics.Drawable;
using Centesimo.Application;

namespace Centesimo.App;

/// <summary>The launcher's long-press menu. Every row is a dynamic shortcut: Android starts a static one in a cleared
/// task, which would destroy an editor that is open.</summary>
internal static class LauncherMenu
{
    /// <summary>Replaces the dynamic shortcuts with <paramref name="shortcuts"/>, ranked in order and cut to what the
    /// launcher shows. False when Android refused the update (rate limited). Never call it on the main thread.</summary>
    public static bool Publish(Context context, WidgetArt art, IReadOnlyList<LauncherShortcut> shortcuts)
    {
        var infos = shortcuts
            .Take(ShortcutManagerCompat.GetMaxShortcutCountPerActivity(context))
            .Select((shortcut, rank) => Info(context, art, shortcut, rank))
            .ToList();
        return ShortcutManagerCompat.SetDynamicShortcuts(context, infos);
    }

    private static ShortcutInfoCompat Info(Context context, WidgetArt art, LauncherShortcut shortcut, int rank)
    {
        var builder = new ShortcutInfoCompat.Builder(context, shortcut.Id);
        builder.SetShortLabel(shortcut.ShortLabel);
        builder.SetLongLabel(shortcut.LongLabel);
        builder.SetRank(rank);
        builder.SetIntent(EntryLinks.Intent(context, shortcut.Entry));
        builder.SetIcon(IconCompat.CreateWithAdaptiveBitmap(art.ShortcutIcon(shortcut.Art)));
        return builder.Build()!;
    }
}
