namespace Centello.App.Theming;

/// <summary>Font aliases: registration and every use read these, so an alias cannot be mistyped.</summary>
public static class Fonts
{
    public const string Regular = "ArchivoRegular";
    public const string SemiBold = "ArchivoSemiBold";
    public const string Bold = "ArchivoBold";
    public const string CondensedExtraBold = "ArchivoCondensedExtraBold";
    public const string Icons = "MaterialSymbols";

    public static IFontCollection AddCentelloFonts(this IFontCollection fonts) => fonts
        .AddFont("Archivo-Regular.ttf", Regular)
        .AddFont("Archivo-SemiBold.ttf", SemiBold)
        .AddFont("Archivo-Bold.ttf", Bold)
        .AddFont("ArchivoCondensed-ExtraBold.ttf", CondensedExtraBold)
        .AddFont("MaterialSymbolsRounded-Filled.ttf", Icons);
}
