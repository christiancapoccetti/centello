namespace Centello.Application;

/// <summary>What one database holds, counted the same way for the backup and the phone. Archived rows count.</summary>
public sealed record DataSummary(IReadOnlySet<Guid> ExpenseIds, int Categories, int Tags, int RecurringPayments)
{
    public int Expenses => ExpenseIds.Count;
}

/// <summary>One line of the comparison: "Spese", "1.284", "1.302".</summary>
public sealed record RestoreComparisonRow(string Label, string InBackup, string OnPhone);

/// <summary>An analysed backup waiting for the red button. <see cref="StagedPath"/> is the checked copy that Restore
/// installs; the page only hands the preview back.</summary>
public sealed record RestorePreview(string StagedPath, DateTimeOffset CreatedAt, DataSummary Backup, DataSummary Phone)
{
    /// <summary>Phone expenses whose id the backup does not hold: what replacing loses. A set difference, so expenses
    /// deleted since the backup do not hide the ones added since.</summary>
    public int LostExpenses => Phone.ExpenseIds.Count(id => !Backup.ExpenseIds.Contains(id));

    /// <summary>"Backup del 12 novembre 2026, 21:40": the wall-clock time it was stamped, whatever the time zone is
    /// now.</summary>
    public string BackupDate =>
        $"Backup {ItalianFormat.OfLongDate(Day)} {Day.Year}, {ItalianFormat.Time(TimeOnly.FromDateTime(CreatedAt.DateTime))}";

    public IReadOnlyList<RestoreComparisonRow> Rows =>
    [
        Row("Spese", summary => summary.Expenses),
        Row("Categorie", summary => summary.Categories),
        Row("Tag", summary => summary.Tags),
        Row("Pagamenti regolari", summary => summary.RecurringPayments)
    ];

    /// <summary>"" when nothing is lost.</summary>
    public string Warning => LostExpenses switch
    {
        0 => "",
        1 => "La spesa che hai sul telefono e non è nel file andrà persa. " + BackUpFirst,
        var count => $"Le {ItalianFormat.Count(count)} spese che hai sul telefono e non sono nel file andranno perse. "
            + BackUpFirst
    };

    public string DoneMessage =>
        $"{(Backup.Expenses == 1 ? "Ripristinata" : "Ripristinate")} {Noun(Backup.Expenses, "spesa", "spese")}, "
        + $"{Noun(Backup.Categories, "categoria", "categorie")}, {Noun(Backup.Tags, "tag", "tag")} e "
        + $"{Noun(Backup.RecurringPayments, "pagamento regolare", "pagamenti regolari")} dal backup {ItalianFormat.OfLongDate(Day)} {Day.Year}.";

    private const string BackUpFirst = "Prima di procedere puoi creare un file di backup dei dati attuali.";

    private DateOnly Day => DateOnly.FromDateTime(CreatedAt.DateTime);

    private RestoreComparisonRow Row(string label, Func<DataSummary, int> count) =>
        new(label, ItalianFormat.Count(count(Backup)), ItalianFormat.Count(count(Phone)));

    private static string Noun(int count, string singular, string plural) =>
        $"{ItalianFormat.Count(count)} {(count == 1 ? singular : plural)}";
}
