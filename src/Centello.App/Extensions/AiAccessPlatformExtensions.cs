using Centello.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App;

public static class AiAccessPlatformExtensions
{
    public static IServiceCollection AddAiAccessPlatform(this IServiceCollection services)
    {
        services.AddSingleton<FeedbackForm>();
#if ANDROID
        services.AddSingleton<IAiAccessStore, AndroidAiAccessStore>();
        services.AddSingleton<IAiRemoteConfig, FirebaseAiRemoteConfig>();
#else
        services.AddSingleton<UnavailableAiAccessPlatform>();
        services.AddSingleton<IAiAccessStore>(provider => provider.GetRequiredService<UnavailableAiAccessPlatform>());
        services.AddSingleton<IAiRemoteConfig>(provider => provider.GetRequiredService<UnavailableAiAccessPlatform>());
#endif
        return services;
    }
}
