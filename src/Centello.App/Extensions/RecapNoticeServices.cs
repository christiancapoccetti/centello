using Centello.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App;

public static class RecapNoticeServices
{
    public static IServiceCollection AddRecapNotices(this IServiceCollection services)
    {
        services.AddSingleton<IRecapNoticeSettings, PreferencesRecapNoticeSettings>();
        services.AddSingleton<RecapNoticeAutomation>();
#if ANDROID
        services.AddSingleton<IRecapNoticeBoard, AndroidRecapNoticeBoard>();
#else
        services.AddSingleton<IRecapNoticeBoard, NoOpRecapNoticeBoard>();
#endif
        return services;
    }
}
