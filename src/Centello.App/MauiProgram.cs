using Centello.App.Theming;
using Centello.Application;
using Centello.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
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
        builder.ConfigureMauiHandlers(handlers => handlers.AddHandler<Shell, CentelloShellRenderer>());
#endif

        // Updates must open the database already present on installations of the published app.
        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "centesimo.db3");
        builder.Services
            .AddApplicationServices()
            .AddInfrastructure(databasePath)
            .AddUi();

        var app = builder.Build();
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<CentelloDbContext>();
        // SQLite may retain a migration lock after an interrupted launch.
        if (dbContext.Database.GetPendingMigrations().Any())
            dbContext.Database.Migrate();

        return app;
    }
}
