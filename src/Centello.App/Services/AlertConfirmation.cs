namespace Centello.App;

public interface IConfirmation
{
    /// <summary>Asks before something that cannot be taken back. True when the person chose
    /// <paramref name="confirm"/>. A destructive confirmation is drawn in the alert color.</summary>
    Task<bool> Ask(string title, string message, string confirm, string cancel, bool isDestructive = false);
}

public static class ConfirmationExtensions
{
    /// <summary>What every editor asks when it is closed with unsaved input. True to discard it.</summary>
    public static Task<bool> AskToDiscard(this IConfirmation confirmation) => confirmation.Ask(
        "Annullare le modifiche?",
        "Quello che hai inserito non verrà salvato.",
        "Annulla modifiche",
        "Continua a modificare");
}

/// <summary>The stock alert, for platforms without a dialog of the design.</summary>
public sealed class AlertConfirmation : IConfirmation
{
    public async Task<bool> Ask(string title, string message, string confirm, string cancel, bool isDestructive = false)
    {
        if (Shell.Current?.CurrentPage is not { } page)
            return false;

        return await page.DisplayAlertAsync(title, message, confirm, cancel);
    }
}
