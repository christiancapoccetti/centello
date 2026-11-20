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

    /// <summary>An expense in a list that spans categories: the category is the title, and the detail says what the
    /// category cannot, in the order day, note (or else tag), "automatica". The day is empty where a group header says
    /// it already.</summary>
    public static (string Title, string Detail) OfExpense(string day, Expense expense, Category? category, Tag? tag) =>
        (NameOf(category), Detail(day, expense.Note.HasValue() ? expense.Note : tag?.Name ?? "", AutomaticMark(expense)));

    /// <summary>An expense in the list of its own category, where the page title already names the category: the note
    /// (or else the category name) is the title, the tag and "automatica" the detail.</summary>
    public static (string Title, string Detail) InCategory(Expense expense, Category? category, Tag? tag) =>
        (Title(expense.Note, NameOf(category)).Title, Detail(tag?.Name ?? "", AutomaticMark(expense)));

    /// <summary>Marks an expense a recurring payment created.</summary>
    public static string AutomaticMark(Expense expense) => expense.IsAutomatic ? "automatica" : "";

    /// <summary>"Ieri · Svago · automatica": the parts that say something, in the order given.</summary>
    public static string Detail(params string[] parts) => Join(" · ", parts);

    /// <summary>"Ogni mese · prossima il 1 dic": a detail that lost its leading category still starts like a sentence.</summary>
    public static string Sentence(string text) =>
        text.Length == 0 ? text : char.ToUpperInvariant(text[0]) + text[1..];

    /// <summary>What a screen reader says for the row: "Netflix, Ieri · Svago · automatica, 15,99 €".</summary>
    public static string Description(params string[] parts) => Join(", ", parts);

    private static string Join(string separator, string[] parts) =>
        string.Join(separator, parts.Where(part => part.HasValue()));
}
