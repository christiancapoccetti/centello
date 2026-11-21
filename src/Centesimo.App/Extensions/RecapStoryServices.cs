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
        services.AddSingleton<StoryTouches>();
#if ANDROID
        services.AddSingleton<IStoryBars, AndroidStoryBars>();
        services.AddSingleton<IScreenReader, AndroidScreenReader>();
#else
        services.AddSingleton<IStoryBars, NoOpStoryBars>();
        services.AddSingleton<IScreenReader, NoScreenReader>();
#endif
        return services;
    }
}
