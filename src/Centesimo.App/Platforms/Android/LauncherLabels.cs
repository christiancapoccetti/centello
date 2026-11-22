namespace Centesimo.App;

internal static class LauncherLabels
{
#if CENTESIMO_DEV
    public const string Prefix = "Dev - ";
#else
    public const string Prefix = "";
#endif
}
