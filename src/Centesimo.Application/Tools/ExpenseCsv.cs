using System.Globalization;
using System.Text;
using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>The spreadsheet export of every expense, shaped for Italian Excel: UTF-8 with a BOM so accents survive,
/// ";" as the separator and a decimal comma, so a double click opens it in columns with numeric amounts. Text that
/// would run as a formula is defused with a leading apostrophe.</summary>
public static class ExpenseCsv
{
    private const string Header = "Data;Importo;Categoria;Tag;Nota;Ricorrente";
    private const string LineEnd = "\r\n";
    private static readonly char[] FormulaStarts = ['=', '+', '-', '@', '\t', '\r'];
    private static readonly char[] QuotedChars = [';', '"', '\r', '\n'];

    public static byte[] Write(IReadOnlyList<Expense> expenses, IReadOnlyList<Category> categories,
        IReadOnlyList<Tag> tags)
    {
        var categoryNames = categories.ToDictionary(category => category.CategoryId, category => category.Name);
        var tagNames = tags.ToDictionary(tag => tag.TagId, tag => tag.Name);
        var text = new StringBuilder(Header).Append(LineEnd);
        foreach (var expense in expenses.OrderBy(expense => expense.OccurredOn).ThenBy(expense => expense.ExpenseId))
        {
            var tagName = expense.TagId.HasValue ? tagNames.GetValueOrDefault(expense.TagId.Value, "") : "";
            text.Append(expense.OccurredOn.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture)).Append(';')
                .Append(ItalianAmount(expense.Amount.Cents)).Append(';')
                .Append(Field(categoryNames.GetValueOrDefault(expense.CategoryId, ""))).Append(';')
                .Append(Field(tagName)).Append(';')
                .Append(Field(expense.Note)).Append(';')
                .Append(expense.IsAutomatic ? "Sì" : "No")
                .Append(LineEnd);
        }

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(text.ToString())];
    }

    private static string ItalianAmount(long cents) =>
        string.Create(CultureInfo.InvariantCulture, $"{cents / 100},{cents % 100:00}");

    private static string Field(string value)
    {
        var guarded = value.Length > 0 && Array.IndexOf(FormulaStarts, value[0]) >= 0 ? "'" + value : value;
        return guarded.IndexOfAny(QuotedChars) < 0 ? guarded : $"\"{guarded.Replace("\"", "\"\"")}\"";
    }
}
