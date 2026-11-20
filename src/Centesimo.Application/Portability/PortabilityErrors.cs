namespace Centesimo.Application;

public static class PortabilityErrors
{
    public static readonly Error ExportFailed = new(
        "Export.Failed",
        "Non è stato possibile esportare le spese.");

    public static readonly Error BackupFailed = new(
        "Backup.Failed",
        "Non è stato possibile creare il file di backup.");
}
