using Android.App;
using Android.Runtime;
using AndroidX.Lifecycle;

namespace Centello.App;

[Android.App.Application]
public class MainApplication : Microsoft.Maui.MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	public override void OnCreate()
	{
		base.OnCreate();
		ProcessLifecycleOwner.Get().Lifecycle.AddObserver(new AiAccessForegroundObserver(IPlatformApplication.Current!.Services));
	}
}
