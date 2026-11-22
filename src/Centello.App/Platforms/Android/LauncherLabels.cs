namespace Centello.App;

internal static class LauncherLabels
{
#if CENTELLO_DEV
    public const string Prefix = "Dev - ";
#else
    public const string Prefix = "";
#endif
}
