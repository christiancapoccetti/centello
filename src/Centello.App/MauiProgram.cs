using Centello.App.Controls;
using Centello.App.Theming;
using Centello.Application;
using Centello.Infrastructure;
using Microsoft.Maui.Storage;

namespace Centello.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddCentelloFonts());
#if ANDROID
        TabularFigures.Register();
        PlainEntryAppearance.Register();
        builder.ConfigureMauiHandlers(handlers => handlers
            .AddHandler<Shell, CentelloShellRenderer>()
            .AddHandler<PlainEntry, PlainEntryHandler>());
#endif

        // Updates must open the database already present on installations of the published app.
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "centesimo.db3");
        builder.Services
            .AddApplicationServices()
            .AddInfrastructure(databasePath)
            .AddUi();

        return builder.Build();
    }
}
