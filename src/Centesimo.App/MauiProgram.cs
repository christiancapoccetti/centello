using Centesimo.App.Theming;
using Centesimo.Application;
using Centesimo.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Maui.Storage;

namespace Centesimo.App;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts => fonts.AddCentesimoFonts());
#if ANDROID
        TabularFigures.Register();
        PlainEntryAppearance.Register();
        builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<Shell, CentesimoShellRenderer>());
#endif

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "centesimo.db3");
        builder.Services
            .AddApplicationServices()
            .AddInfrastructure(databasePath)
            .AddUi();

        var app = builder.Build();
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CentesimoDbContext>();
        dbContext.Database.Migrate();
        return app;
    }
}
