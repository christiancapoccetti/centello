namespace Centesimo.App;

/// <summary>The offer to send a reminder before a recurring payment. It is made in the editor of a new payment and
/// only until it is seen through, so nobody is asked again after every save.</summary>
public interface IReminderOffer
{
    /// <summary>True while notifications are not allowed and the offer was never seen through.</summary>
    Task<bool> IsOpen();

    /// <summary>The person saved a payment with the offer in view, or answered it: it is not made again.</summary>
    void Close();

    /// <summary>Asks the system for the notification permission and closes the offer. True when granted.</summary>
    Task<bool> Accept();
}

public sealed class NotificationReminderOffer : IReminderOffer
{
    private const string ClosedKey = "reminder-offer-closed";

    public async Task<bool> IsOpen()
    {
        if (Preferences.Default.Get(ClosedKey, false))
            return false;

        return await Permissions.CheckStatusAsync<Permissions.PostNotifications>() != PermissionStatus.Granted;
    }

    public void Close() => Preferences.Default.Set(ClosedKey, true);

    public async Task<bool> Accept()
    {
        Close();
        return await Permissions.RequestAsync<Permissions.PostNotifications>() == PermissionStatus.Granted;
    }
}
