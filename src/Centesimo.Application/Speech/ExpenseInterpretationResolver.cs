using System.Globalization;

namespace Centesimo.Application;

/// <summary>Turns model output into a draft, trusting nothing the model returned.</summary>
public static class ExpenseInterpretationResolver
{
    public const int MaximumNoteLength = 500;
    private const string DateFormat = "yyyy-MM-dd";

    public static ExpenseSpeechDraft Resolve(
        ExpenseInterpretation value,
        ExpenseInterpretationCatalog catalog,
        DateOnly today,
        string sourceText)
    {
        var category = catalog.FindCategory(value.CategoryKey);
        var tag = category?.FindTag(value.TagKey);
        return new ExpenseSpeechDraft(
            ToCents(value.Amount),
            category?.Category.CategoryId,
            tag?.Tag.TagId,
            ParseDate(value.Date, today),
            LimitNote(value.Note.Trim()),
            sourceText);
    }

    private static long? ToCents(decimal? amount)
    {
        if (amount is not { } value || value <= 0 || value > long.MaxValue / 100m)
            return null;

        var cents = value * 100;
        return decimal.Truncate(cents) == cents ? decimal.ToInt64(cents) : null;
    }

    private static DateOnly? ParseDate(string? text, DateOnly today) =>
        DateOnly.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) && date <= today
            ? date
            : null;

    private static string LimitNote(string note)
    {
        if (note.Length <= MaximumNoteLength)
            return note;

        var length = char.IsHighSurrogate(note[MaximumNoteLength - 1]) ? MaximumNoteLength - 1 : MaximumNoteLength;
        return note[..length].TrimEnd();
    }
}
