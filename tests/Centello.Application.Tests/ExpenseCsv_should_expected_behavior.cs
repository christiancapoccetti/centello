using System.Text;
using Centello.Domain;

namespace Centello.Application.Tests;

public sealed class ExpenseCsv_should_expected_behavior
{
    private static readonly Category Food = new(Guid.NewGuid(), "Cibo", "cart", "#176B5B");
    private static readonly Tag Lunch = new(Guid.NewGuid(), Food.CategoryId, "Pranzo");

    private static string[] Lines(params Expense[] expenses) => Lines(expenses, [Food], [Lunch]);

    private static string[] Lines(IReadOnlyList<Expense> expenses, IReadOnlyList<Category> categories,
        IReadOnlyList<Tag> tags) =>
        Text(ExpenseCsv.Write(expenses, categories, tags)).Split("\r\n");

    private static string Text(byte[] bytes) => new UTF8Encoding(false).GetString(bytes, 3, bytes.Length - 3);

    private static Expense Spent(long cents, string note = "", DateOnly? on = null, Guid? tagId = null,
        Guid? recurringPaymentId = null, Guid? id = null) =>
        new(id ?? Guid.NewGuid(), Food.CategoryId, new Money(cents), on ?? new DateOnly(2026, 11, 5),
            tagId, note, null, recurringPaymentId);

    [Fact]
    public void Start_with_the_utf8_byte_order_mark_and_the_header()
    {
        var bytes = ExpenseCsv.Write([], [], []);

        Assert.Equal([0xEF, 0xBB, 0xBF], bytes[..3]);
        Assert.Equal("Data;Importo;Categoria;Tag;Nota;Ricorrente\r\n", Text(bytes));
    }

    [Fact]
    public void Write_the_header_only_for_an_empty_list()
    {
        Assert.Equal(["Data;Importo;Categoria;Tag;Nota;Ricorrente", ""], Lines([], [Food], [Lunch]));
    }

    [Fact]
    public void End_every_line_with_crlf_including_the_last()
    {
        var text = Text(ExpenseCsv.Write([Spent(100)], [Food], [Lunch]));

        Assert.EndsWith("\r\n", text);
        Assert.Equal(2, text.Split("\r\n").Length - 1);
        Assert.DoesNotContain("sep=", text);
        Assert.DoesNotContain("\n", text.Replace("\r\n", ""));
    }

    [Theory]
    [InlineData(128460L, "1284,60")]
    [InlineData(5L, "0,05")]
    [InlineData(100L, "1,00")]
    [InlineData(99L, "0,99")]
    [InlineData(123456789012L, "1234567890,12")]
    public void Write_amounts_with_a_decimal_comma_and_no_thousands_separator(long cents, string expected)
    {
        var fields = Lines(Spent(cents))[1].Split(';');

        Assert.Equal(expected, fields[1]);
    }

    [Fact]
    public void Write_the_date_as_day_month_year()
    {
        Assert.StartsWith("05/11/2026;", Lines(Spent(100, on: new DateOnly(2026, 11, 5)))[1]);
    }

    [Fact]
    public void Write_the_category_and_the_tag_names_of_archived_ones_too()
    {
        var archivedCategory = new Category(Guid.NewGuid(), "Vecchia", "cart", "#176B5B");
        archivedCategory.Archive();
        var archivedTag = new Tag(Guid.NewGuid(), archivedCategory.CategoryId, "Antico");
        archivedTag.Archive();
        var expense = new Expense(Guid.NewGuid(), archivedCategory.CategoryId, new Money(100),
            new DateOnly(2026, 1, 1), archivedTag.TagId);

        var lines = Lines([expense], [archivedCategory], [archivedTag]);

        Assert.Equal("01/01/2026;1,00;Vecchia;Antico;;No", lines[1]);
    }

    [Fact]
    public void Leave_the_tag_empty_when_the_expense_has_none()
    {
        Assert.Equal("05/11/2026;1,00;Cibo;;;No", Lines(Spent(100))[1]);
    }

    [Fact]
    public void Mark_automatic_expenses_as_recurring()
    {
        var lines = Lines(Spent(100, recurringPaymentId: Guid.NewGuid()), Spent(200, tagId: Lunch.TagId));

        Assert.Contains(lines.Skip(1).Take(2), line => line.EndsWith(";Sì"));
        Assert.Contains(lines.Skip(1).Take(2), line => line.EndsWith(";No"));
    }

    [Fact]
    public void Quote_a_field_with_the_separator_and_double_the_inner_quotes()
    {
        var lines = Lines(Spent(100, "pane; latte \"bio\""));

        Assert.Equal("05/11/2026;1,00;Cibo;;\"pane; latte \"\"bio\"\"\";No", lines[1]);
    }

    [Fact]
    public void Keep_a_two_line_note_inside_one_quoted_field()
    {
        var text = Text(ExpenseCsv.Write([Spent(100, "prima\r\nseconda")], [Food], [Lunch]));

        Assert.Contains(";\"prima\r\nseconda\";No\r\n", text);
    }

    [Theory]
    [InlineData("=SOMMA(A1)", "'=SOMMA(A1)")]
    [InlineData("+39 333", "'+39 333")]
    [InlineData("-5 euro", "'-5 euro")]
    [InlineData("@utente", "'@utente")]
    public void Defuse_a_note_that_starts_like_a_formula(string note, string expected)
    {
        Assert.Equal($"05/11/2026;1,00;Cibo;;{expected};No", Lines(Spent(100, note))[1]);
    }

    [Fact]
    public void Defuse_a_formula_in_the_category_and_tag_names()
    {
        var category = new Category(Guid.NewGuid(), "=Cat", "cart", "#176B5B");
        var tag = new Tag(Guid.NewGuid(), category.CategoryId, "@Tag");
        var expense = new Expense(Guid.NewGuid(), category.CategoryId, new Money(100),
            new DateOnly(2026, 1, 1), tag.TagId);

        Assert.Equal("01/01/2026;1,00;'=Cat;'@Tag;;No", Lines([expense], [category], [tag])[1]);
    }

    [Fact]
    public void Defuse_before_quoting()
    {
        Assert.Equal("05/11/2026;1,00;Cibo;;\"'=a;b\";No", Lines(Spent(100, "=a;b"))[1]);
    }

    [Fact]
    public void Write_the_oldest_expenses_first_and_break_ties_by_id()
    {
        var low = Guid.Parse("00000000-0000-0000-0000-000000000001");
        var high = Guid.Parse("00000000-0000-0000-0000-000000000002");
        var lines = Lines(
            Spent(300, on: new DateOnly(2026, 11, 7), id: Guid.NewGuid()),
            Spent(200, on: new DateOnly(2026, 11, 5), id: high),
            Spent(100, on: new DateOnly(2026, 11, 5), id: low),
            Spent(400, on: new DateOnly(2025, 1, 1), id: Guid.NewGuid()));

        Assert.Equal(["01/01/2025;4,00", "05/11/2026;1,00", "05/11/2026;2,00", "07/11/2026;3,00"],
            lines.Skip(1).Take(4).Select(line => string.Join(';', line.Split(';')[..2])));
    }
}
