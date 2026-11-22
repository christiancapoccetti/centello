namespace Centello.Application.Tests;

public sealed class ExpenseInterpretationResolver_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 19);
    private static readonly Guid CarId = Guid.NewGuid();
    private static readonly Guid FuelId = Guid.NewGuid();
    private static readonly Guid HomeId = Guid.NewGuid();
    private static readonly Guid RentId = Guid.NewGuid();

    private static readonly ExpenseInterpretationCatalog Catalog = new(
    [
        new DraftCategory(CarId, "Auto", [new DraftTag(FuelId, "Carburante")]),
        new DraftCategory(HomeId, "Casa", [new DraftTag(RentId, "Affitto")])
    ]);

    private static ExpenseInterpretation Interpretation(
        decimal? amount = 100m,
        string? categoryKey = "c1",
        string? tagKey = "t1",
        string? date = null,
        string note = "") =>
        new(amount, categoryKey, tagKey, date, note);

    private static ExpenseDraft Resolve(ExpenseInterpretation value) =>
        ExpenseInterpretationResolver.Resolve(value, Catalog, Today, DraftSource.Voice, "Ho speso 100 euro di carburante");

    [Fact]
    public void Resolve_a_complete_interpretation_to_a_draft_with_canonical_ids()
    {
        var draft = Resolve(Interpretation(date: "2026-11-18", note: "pieno"));

        Assert.Equal(10_000, draft.AmountCents);
        Assert.Equal(CarId, draft.CategoryId);
        Assert.Equal(FuelId, draft.TagId);
        Assert.Equal(new DateOnly(2026, 11, 18), draft.OccurredOn);
        Assert.Equal("pieno", draft.Note);
        Assert.Equal("Ho speso 100 euro di carburante", draft.Heard);
    }

    [Fact]
    public void Keep_the_source_and_what_was_heard_on_the_draft()
    {
        var draft = ExpenseInterpretationResolver.Resolve(Interpretation(), Catalog, Today, DraftSource.Voice, "cento euro");

        Assert.Same(DraftSource.Voice, draft.Source);
        Assert.Equal("cento euro", draft.Heard);
        Assert.Equal("Dalla voce: “cento euro” · controlla e salva", draft.Banner);
    }

    [Fact]
    public void Resolve_a_photo_draft_with_nothing_heard()
    {
        var draft = ExpenseInterpretationResolver.Resolve(Interpretation(), Catalog, Today, DraftSource.Photo);

        Assert.Same(DraftSource.Photo, draft.Source);
        Assert.Equal("", draft.Heard);
        Assert.Equal("Dalla foto · controlla e salva", draft.Banner);
    }

    [Fact]
    public void Leave_category_and_tag_empty_when_the_model_found_no_match()
    {
        var draft = Resolve(Interpretation(categoryKey: null, tagKey: null));

        Assert.Null(draft.CategoryId);
        Assert.Null(draft.TagId);
        Assert.Equal(10_000, draft.AmountCents);
    }

    [Theory]
    [InlineData("c9")]
    [InlineData("")]
    [InlineData("Auto")]
    [InlineData("t1")]
    public void Drop_category_and_tag_when_the_category_key_does_not_exist(string categoryKey)
    {
        var draft = Resolve(Interpretation(categoryKey: categoryKey, tagKey: "t1"));

        Assert.Null(draft.CategoryId);
        Assert.Null(draft.TagId);
    }

    [Fact]
    public void Drop_the_tag_when_it_belongs_to_another_category_but_keep_the_category()
    {
        var draft = Resolve(Interpretation(categoryKey: "c1", tagKey: "t2"));

        Assert.Equal(CarId, draft.CategoryId);
        Assert.Null(draft.TagId);
    }

    [Fact]
    public void Drop_a_tag_key_that_does_not_exist_but_keep_the_category()
    {
        var draft = Resolve(Interpretation(tagKey: "t42"));

        Assert.Equal(CarId, draft.CategoryId);
        Assert.Null(draft.TagId);
    }

    [Theory]
    [InlineData("12.5", 1_250)]
    [InlineData("0.01", 1)]
    [InlineData("100", 10_000)]
    public void Convert_valid_amounts_to_cents(string amount, long cents)
    {
        var draft = Resolve(Interpretation(amount: decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Equal(cents, draft.AmountCents);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("12.345")]
    [InlineData("0.001")]
    [InlineData("79228162514264337593543950335")]
    public void Leave_the_amount_empty_when_it_is_not_a_valid_euro_amount(string amount)
    {
        var draft = Resolve(Interpretation(amount: decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)));

        Assert.Null(draft.AmountCents);
        Assert.Equal(CarId, draft.CategoryId);
    }

    [Fact]
    public void Leave_the_amount_empty_when_the_model_found_none()
    {
        var draft = Resolve(Interpretation(amount: null));

        Assert.Null(draft.AmountCents);
    }

    [Fact]
    public void Accept_today_as_the_expense_date()
    {
        var draft = Resolve(Interpretation(date: "2026-11-19"));

        Assert.Equal(Today, draft.OccurredOn);
    }

    [Theory]
    [InlineData("2026-11-20")]
    [InlineData("2027-01-01")]
    public void Ignore_a_date_in_the_future(string date)
    {
        var draft = Resolve(Interpretation(date: date));

        Assert.Null(draft.OccurredOn);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ieri")]
    [InlineData("18/11/2026")]
    [InlineData("2026-1-5")]
    [InlineData("2026-02-30")]
    [InlineData("2026-11-18T10:00:00")]
    [InlineData(" 2026-11-18")]
    public void Ignore_a_date_that_is_not_strict_iso(string? date)
    {
        var draft = Resolve(Interpretation(date: date));

        Assert.Null(draft.OccurredOn);
    }

    [Fact]
    public void Trim_the_note()
    {
        var draft = Resolve(Interpretation(note: "  pieno completo \n"));

        Assert.Equal("pieno completo", draft.Note);
    }

    [Fact]
    public void Truncate_a_long_note_to_the_editor_limit()
    {
        var draft = Resolve(Interpretation(note: new string('a', ExpenseInterpretationResolver.MaximumNoteLength + 50)));

        Assert.Equal(ExpenseInterpretationResolver.MaximumNoteLength, draft.Note.Length);
    }

    [Fact]
    public void Keep_a_note_that_fits_the_editor_limit_untouched()
    {
        var note = new string('a', ExpenseInterpretationResolver.MaximumNoteLength);

        Assert.Equal(note, Resolve(Interpretation(note: note)).Note);
    }

    [Fact]
    public void Not_split_a_surrogate_pair_when_truncating_the_note()
    {
        var note = new string('a', ExpenseInterpretationResolver.MaximumNoteLength - 1) + "😀";

        var draft = Resolve(Interpretation(note: note));

        Assert.Equal(ExpenseInterpretationResolver.MaximumNoteLength - 1, draft.Note.Length);
        Assert.DoesNotContain(draft.Note, char.IsSurrogate);
    }
}
