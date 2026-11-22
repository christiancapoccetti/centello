using Centello.Application;
using Microsoft.Maui.Networking;

namespace Centello.App;

public sealed class MauiInternetAccess : IInternetAccess
{
    public bool IsAvailable => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
}
