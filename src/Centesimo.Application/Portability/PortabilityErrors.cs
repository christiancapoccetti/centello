namespace Centesimo.Application;

public static class PortabilityErrors
{
    public static readonly Error ExportFailed = new(
        "Export.Failed",
        "Non è stato possibile esportare le spese.");
}
