namespace Centesimo.Application;

public static class PortabilityErrors
{
    public static readonly Error ExportFailed = new(
        "Export.Failed",
        "Non è stato possibile esportare le spese.");

    public static readonly Error BackupFailed = new(
        "Backup.Failed",
        "Non è stato possibile creare il file di backup.");

    public static readonly Error NotABackup = new(
        "Backup.NotABackup",
        "Questo file non è un backup di Centesimo. Scegli un file creato con “Crea file di backup”.");

    public static readonly Error NewerVersion = new(
        "Backup.NewerVersion",
        "Questo backup viene da una versione più recente di Centesimo. Aggiorna l'app e riprova.");

    public static readonly Error Damaged = new(
        "Backup.Damaged",
        "Il file di backup è danneggiato e non può essere ripristinato.");

    public static readonly Error RestoreFailed = new(
        "Backup.RestoreFailed",
        "Non è stato possibile ripristinare il backup. I dati del telefono non sono cambiati.");
}
