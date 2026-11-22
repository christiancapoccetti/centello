using Centello.App.Pages;
using Centello.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App;

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
