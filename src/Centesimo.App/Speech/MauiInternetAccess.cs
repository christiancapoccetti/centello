using Centesimo.Application;
using Microsoft.Maui.Networking;

namespace Centesimo.App;

public sealed class MauiInternetAccess : IInternetAccess
{
    public bool IsAvailable => Connectivity.Current.NetworkAccess == NetworkAccess.Internet;
}
