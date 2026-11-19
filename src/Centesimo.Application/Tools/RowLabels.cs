using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>How a list row names an expense or a recurring payment. Every list reads the rule here, so a row says the
/// same thing wherever it appears.</summary>
internal static class RowLabels
{
    private const string MissingCategory = "Categoria non disponibile";

    public static string NameOf(Category? category) => category?.Name ?? MissingCategory;

    /// <summary>The title is the note, or the category name when there is no note. <c>Category</c> is what the detail
    /// may still say about the category: nothing when the title already is its name, so a row never names it twice.</summary>
    public static (string Title, string Category) Title(string note, string categoryName) =>
        note.HasValue() ? (note, categoryName) : (categoryName, "");

    /// <summary>Marks an expense a recurring payment created.</summary>
    public static string AutomaticMark(Expense expense) => expense.RecurringPaymentId.HasValue ? "automatica" : "";

    /// <summary>"Ieri · Svago · automatica": the parts that say something, in the order given.</summary>
    public static string Detail(params string[] parts) => Join(" · ", parts);

    /// <summary>What a screen reader says for the row: "Netflix, Ieri · Svago · automatica, 15,99 €".</summary>
    public static string Description(params string[] parts) => Join(", ", parts);

    private static string Join(string separator, string[] parts) =>
        string.Join(separator, parts.Where(part => part.HasValue()));
}
