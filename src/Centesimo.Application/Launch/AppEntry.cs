using System.Web;

namespace Centesimo.Application;

/// <summary>Where a tap from outside the app (a widget, the voice panel) asks it to open. It travels as the data of
/// the intent that starts the app. Parse is the only way back from a link; a link it does not know is null.</summary>
public abstract record AppEntry
{
    private const string Scheme = "centesimo";
    private const string CategoryKey = "category";

    private AppEntry()
    {
    }

    /// <summary>Oggi, on the month of today.</summary>
    public sealed record Today : AppEntry;

    /// <summary>The editor of a new expense, amount first. A category no longer active is dropped by the editor.</summary>
    public sealed record NewExpense(Guid? CategoryId) : AppEntry;

    /// <summary>The editor of a new expense, filled with the draft the voice panel left in IPendingExpenseSpeechDraft.</summary>
    public sealed record VoiceDraft : AppEntry;

    /// <summary>Impostazioni, where the OpenAI key is set.</summary>
    public sealed record Settings : AppEntry;

    public string ToLink() => this switch
    {
        Today => $"{Scheme}://today",
        NewExpense { CategoryId: { } categoryId } => $"{Scheme}://expense/new?{CategoryKey}={categoryId:D}",
        NewExpense => $"{Scheme}://expense/new",
        VoiceDraft => $"{Scheme}://expense/voice",
        Settings => $"{Scheme}://settings",
        _ => throw new InvalidOperationException("Unknown app entry.")
    };

    public static AppEntry? Parse(string? link)
    {
        if (link is null || !link.StartsWith($"{Scheme}://", StringComparison.Ordinal))
            return null;

        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri))
            return null;

        return $"{uri.Host}{uri.AbsolutePath}".TrimEnd('/') switch
        {
            "today" => new Today(),
            "expense/new" => new NewExpense(CategoryOf(uri)),
            "expense/voice" => new VoiceDraft(),
            "settings" => new Settings(),
            _ => null
        };
    }

    private static Guid? CategoryOf(Uri uri) =>
        Guid.TryParse(HttpUtility.ParseQueryString(uri.Query)[CategoryKey], out var categoryId)
            ? categoryId
            : null;
}
