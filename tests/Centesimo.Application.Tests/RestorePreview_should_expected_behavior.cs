namespace Centesimo.Application.Tests;

public sealed class RestorePreview_should_expected_behavior
{
    private static readonly DateTimeOffset CreatedAt = new(2026, 11, 12, 21, 40, 5, TimeSpan.FromHours(1));

    private static IReadOnlySet<Guid> Ids(int count) => Enumerable.Range(0, count).Select(_ => Guid.NewGuid()).ToHashSet();

    private static DataSummary Summary(IReadOnlySet<Guid> ids, int categories = 0, int tags = 0, int payments = 0) =>
        new(ids, categories, tags, payments);

    private static RestorePreview Preview(DataSummary backup, DataSummary phone, DateTimeOffset? createdAt = null) =>
        new("staged.db3", createdAt ?? CreatedAt, backup, phone);

    [Fact]
    public void Count_the_phone_expenses_the_backup_does_not_hold()
    {
        var shared = Ids(3);
        var preview = Preview(Summary(shared), Summary(shared.Concat(Ids(2)).ToHashSet()));

        Assert.Equal(2, preview.LostExpenses);
    }

    [Fact]
    public void Count_a_lost_expense_even_when_the_phone_holds_fewer_expenses()
    {
        var shared = Ids(3);
        var deleted = shared.Take(2).ToHashSet();
        var phone = shared.Except(deleted).Concat(Ids(1)).ToHashSet();

        var preview = Preview(Summary(shared), Summary(phone));

        Assert.Equal(1, preview.LostExpenses);
        Assert.True(preview.Phone.Expenses < preview.Backup.Expenses);
    }

    [Fact]
    public void Lose_nothing_when_the_phone_holds_the_same_expenses()
    {
        var shared = Ids(3);

        Assert.Equal(0, Preview(Summary(shared), Summary(shared)).LostExpenses);
    }

    [Theory]
    [InlineData(2026, 11, 12, 21, 40, "Backup del 12 novembre 2026, 21:40")]
    [InlineData(2026, 8, 8, 9, 5, "Backup dell'8 agosto 2026, 09:05")]
    [InlineData(2026, 11, 11, 0, 0, "Backup dell'11 novembre 2026, 00:00")]
    public void Date_the_backup_by_the_time_it_was_stamped(int year, int month, int day, int hour, int minute,
        string expected)
    {
        var createdAt = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.FromHours(-7));

        Assert.Equal(expected, Preview(Summary(Ids(0)), Summary(Ids(0)), createdAt).BackupDate);
    }

    [Fact]
    public void Compare_the_counts_row_by_row()
    {
        var preview = Preview(
            Summary(Ids(1_284), 23, 41, 6),
            Summary(Ids(1_302), 24, 43, 6));

        Assert.Equal(
            [
                new RestoreComparisonRow("Spese", "1.284", "1.302"),
                new RestoreComparisonRow("Categorie", "23", "24"),
                new RestoreComparisonRow("Tag", "41", "43"),
                new RestoreComparisonRow("Pagamenti regolari", "6", "6")
            ],
            preview.Rows);
    }

    [Fact]
    public void Not_warn_when_nothing_is_lost()
    {
        var shared = Ids(2);

        Assert.Equal("", Preview(Summary(shared), Summary(shared)).Warning);
    }

    [Fact]
    public void Warn_about_one_lost_expense_in_the_singular()
    {
        var preview = Preview(Summary(Ids(2)), Summary(Ids(1)));

        Assert.Equal(
            "La spesa che hai sul telefono e non è nel file andrà persa. "
            + "Prima di procedere puoi creare un file di backup dei dati attuali.",
            preview.Warning);
    }

    [Fact]
    public void Warn_about_many_lost_expenses_in_the_plural()
    {
        var preview = Preview(Summary(Ids(2)), Summary(Ids(18)));

        Assert.Equal(
            "Le 18 spese che hai sul telefono e non sono nel file andranno perse. "
            + "Prima di procedere puoi creare un file di backup dei dati attuali.",
            preview.Warning);
    }

    [Fact]
    public void Say_what_was_restored()
    {
        var preview = Preview(Summary(Ids(1_284), 23, 41, 6), Summary(Ids(0)));

        Assert.Equal(
            "Ripristinate 1.284 spese, 23 categorie, 41 tag e 6 pagamenti regolari dal backup del 12 novembre 2026.",
            preview.DoneMessage);
    }

    [Fact]
    public void Say_what_was_restored_in_the_singular()
    {
        var createdAt = new DateTimeOffset(2026, 8, 8, 9, 5, 0, TimeSpan.Zero);
        var preview = Preview(Summary(Ids(1), 1, 1, 1), Summary(Ids(0)), createdAt);

        Assert.Equal(
            "Ripristinata 1 spesa, 1 categoria, 1 tag e 1 pagamento regolare dal backup dell'8 agosto 2026.",
            preview.DoneMessage);
    }
}
