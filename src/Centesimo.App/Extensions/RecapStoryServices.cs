using Centesimo.App.Pages;
using Centesimo.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App;

public static class RecapStoryServices
{
    public static IServiceCollection AddRecapStory(this IServiceCollection services)
    {
        services.AddTransient<RecapViewModel>();
        services.AddTransient<RecapPage>();
#if ANDROID
        services.AddSingleton<IStoryBars, AndroidStoryBars>();
#else
        services.AddSingleton<IStoryBars, NoOpStoryBars>();
#endif
        return services;
    }
}
