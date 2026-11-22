namespace Centello.Domain;

public sealed class Expense
{
    public Guid ExpenseId { get; }
    public Guid CategoryId { get; }
    public Guid? TagId { get; }
    public Money Amount { get; }
    public DateOnly OccurredOn { get; }
    public string Note { get; }
    public string? PhotoPath { get; }
    public Guid? RecurringPaymentId { get; }

    /// <summary>Created by a recurring payment. The link survives edits and cannot be cleared, so an edited automatic
    /// expense stays automatic. Imported and typed-in expenses never are. Expression-bodied, so EF does not map it.</summary>
    public bool IsAutomatic => RecurringPaymentId.HasValue;

    public Expense(Guid expenseId, Guid categoryId, Money amount, DateOnly occurredOn,
        Guid? tagId = null, string note = "", string? photoPath = null, Guid? recurringPaymentId = null)
    {
        if (expenseId == Guid.Empty || categoryId == Guid.Empty)
            throw new ArgumentException("Expense and category IDs are required.");

        if (amount.Cents == 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Expense amount must be greater than zero.");

        ExpenseId = expenseId;
        CategoryId = categoryId;
        Amount = amount;
        OccurredOn = occurredOn;
        TagId = tagId;
        Note = note.Trim();
        PhotoPath = photoPath;
        RecurringPaymentId = recurringPaymentId;
    }
}
