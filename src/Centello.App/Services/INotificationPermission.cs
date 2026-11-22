namespace Centello.App;

/// <summary>The one way the app asks for the permission to notify: the payment reminder offer and the Settings switch
/// of the recap both go through it.</summary>
public interface INotificationPermission
{
    Task<bool> IsGranted();

    /// <summary>Asks the system. True when granted, also when it was granted before.</summary>
    Task<bool> Request();
}

public sealed class MauiNotificationPermission : INotificationPermission
{
    public async Task<bool> IsGranted() =>
        await Permissions.CheckStatusAsync<Permissions.PostNotifications>() == PermissionStatus.Granted;

    public async Task<bool> Request() =>
        await Permissions.RequestAsync<Permissions.PostNotifications>() == PermissionStatus.Granted;
}
