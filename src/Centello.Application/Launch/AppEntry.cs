using System.Web;

namespace Centello.Application;

/// <summary>Where a tap from outside the app (a widget, a launcher shortcut, the voice panel) asks it to go. It travels
/// as the data of the intent that starts it. A Screen opens inside the app, a Panel opens over the launcher. Pinned
/// launcher shortcuts keep their link forever, so a shipped link is parsed by every later version. Parse is the only
/// way back from a link; a link it does not know is null.</summary>
public abstract record AppEntry
{
    private const string Scheme = "centello";
    private const string CategoryKey = "category";
    private const string YearKey = "year";
    private const string MonthKey = "month";

    private AppEntry()
    {
    }

    /// <summary>True when the entry starts the capture of an expense, so it needs a category to exist.</summary>
    public bool AddsExpense => this is Screen.NewExpense or Panel;

    public string ToLink() => this switch
    {
        Screen.Today => $"{Scheme}://today",
        Screen.NewExpense { CategoryId: { } categoryId } => $"{Scheme}://expense/new?{CategoryKey}={categoryId:D}",
        Screen.NewExpense => $"{Scheme}://expense/new",
        Screen.Draft => $"{Scheme}://expense/draft",
        Screen.Settings => $"{Scheme}://settings",
        Screen.AiInvite => $"{Scheme}://settings/ai-invite",
        Screen.Recap { Period: var period } => RecapLink(period),
        Panel.VoicePanel => $"{Scheme}://panel/voice",
        Panel.PhotoPanel => $"{Scheme}://panel/photo",
        _ => throw new InvalidOperationException("Unknown app entry.")
    };

    public static AppEntry? Parse(string? link)
    {
        if (link is null || (!link.StartsWith($"{Scheme}://", StringComparison.Ordinal)
            && !link.StartsWith("centesimo://", StringComparison.Ordinal)))
            return null;

        if (!Uri.TryCreate(link, UriKind.Absolute, out var uri))
            return null;

        return $"{uri.Host}{uri.AbsolutePath}".TrimEnd('/') switch
        {
            "today" => new Screen.Today(),
            "expense/new" => new Screen.NewExpense(CategoryOf(uri)),
            "expense/draft" => new Screen.Draft(),
            "settings" => new Screen.Settings(),
            "settings/ai-invite" => new Screen.AiInvite(),
            "recap" => RecapOf(uri),
            "panel/voice" => new Panel.VoicePanel(),
            "panel/photo" => new Panel.PhotoPanel(),
            _ => null
        };
    }

    private static string RecapLink(Period period) =>
        $"{Scheme}://recap?{YearKey}={period.Start.Year}"
        + (period.Kind == PeriodKind.Month ? $"&{MonthKey}={period.Start.Month}" : "");

    private static Screen.Recap? RecapOf(Uri uri)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        if (!int.TryParse(query[YearKey], out var year) || year is < 2000 or > 2999)
            return null;

        var month = query[MonthKey];
        if (month is null)
            return new Screen.Recap(Period.Year(year));

        return int.TryParse(month, out var number) && number is >= 1 and <= 12
            ? new Screen.Recap(Period.Month(year, number))
            : null;
    }

    private static Guid? CategoryOf(Uri uri) =>
        Guid.TryParse(HttpUtility.ParseQueryString(uri.Query)[CategoryKey], out var categoryId)
            ? categoryId
            : null;

    /// <summary>An entry the app opens on a page of its own.</summary>
    public abstract record Screen : AppEntry
    {
        private protected Screen()
        {
        }

        /// <summary>Oggi, on the month of today.</summary>
        public sealed record Today : Screen;

        /// <summary>The editor of a new expense, amount first. A category no longer active is dropped by the editor.</summary>
        public sealed record NewExpense(Guid? CategoryId) : Screen;

        /// <summary>The editor of a new expense, filled with the draft a panel handed to the app.</summary>
        public sealed record Draft : Screen;

        public sealed record Settings : Screen;

        public sealed record AiInvite : Screen;

        /// <summary>The story of a month or a year, opened over Insight on that period.</summary>
        public sealed record Recap(Period Period) : Screen;
    }

    /// <summary>An entry that opens a panel over the launcher, in a task of its own.</summary>
    public abstract record Panel : AppEntry
    {
        private protected Panel()
        {
        }

        public sealed record VoicePanel : Panel;

        public sealed record PhotoPanel : Panel;
    }
}
