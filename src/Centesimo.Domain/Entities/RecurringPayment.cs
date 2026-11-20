namespace Centesimo.Domain;

public sealed class RecurringPayment
{
    public Guid RecurringPaymentId { get; }
    public Guid CategoryId { get; private set; }
    public Guid? TagId { get; private set; }
    public Money Amount { get; private set; }
    public string Note { get; private set; } = "";
    public RecurrenceFrequency Frequency { get; private set; }
    public DateOnly NextDueOn { get; private set; }
    public DateOnly? EndsOn { get; private set; }
    public bool IsSuspended { get; private set; }
    public int AnchorMonth { get; private set; }
    public int AnchorDay { get; private set; }

    /// <summary>True once the next occurrence falls after the end date, so no further expense will be created.</summary>
    public bool IsEnded => EndsOn.HasValue && NextDueOn > EndsOn.Value;

    /// <summary>What the payment is doing now. A payment past its end is ended even if it was paused before.</summary>
    public RecurringPaymentStatus Status => IsEnded
        ? RecurringPaymentStatus.Ended
        : IsSuspended ? RecurringPaymentStatus.Paused : RecurringPaymentStatus.Active;

    // For loading a stored payment. The public constructor checks what a new schedule must satisfy, and a payment
    // that has ended is stored with its next occurrence after its end date, which a new schedule may not have.
    private RecurringPayment()
    {
    }

    public RecurringPayment(Guid recurringPaymentId, Guid categoryId, Money amount,
        RecurrenceFrequency frequency, DateOnly nextDueOn, Guid? tagId = null,
        string note = "", DateOnly? endsOn = null)
    {
        if (recurringPaymentId == Guid.Empty || categoryId == Guid.Empty)
            throw new ArgumentException("Recurring payment and category IDs are required.");

        if (amount.Cents == 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");

        RecurringPaymentId = recurringPaymentId;
        AnchorMonth = nextDueOn.Month;
        AnchorDay = nextDueOn.Day;
        Update(categoryId, amount, frequency, nextDueOn, tagId, note, endsOn);
    }

    public void Update(Guid categoryId, Money amount, RecurrenceFrequency frequency,
        DateOnly nextDueOn, Guid? tagId, string note, DateOnly? endsOn)
    {
        if (categoryId == Guid.Empty || amount.Cents == 0)
            throw new ArgumentException("Category and positive amount are required.");

        if (endsOn < nextDueOn)
            throw new ArgumentException("End date cannot precede the next due date.", nameof(endsOn));

        CategoryId = categoryId;
        Amount = amount;
        Frequency = frequency;
        NextDueOn = nextDueOn;
        TagId = tagId;
        Note = note.Trim();
        EndsOn = endsOn;
        AnchorMonth = nextDueOn.Month;
        AnchorDay = nextDueOn.Day;
    }

    public void Suspend() => IsSuspended = true;

    /// <summary>Resumes a paused payment from the first occurrence on or after <paramref name="today"/>. The occurrences
    /// missed during the pause are skipped, not created late. A payment whose end date passes while skipping is ended.</summary>
    public void Resume(DateOnly today)
    {
        if (!IsSuspended)
            return;

        IsSuspended = false;
        NextDueOn = UpcomingDueOn(today);
    }

    /// <summary>The first occurrence on or after <paramref name="today"/>, or the first one past the end date when
    /// the payment ends before then. The schedule is left where it is.</summary>
    public DateOnly UpcomingDueOn(DateOnly today)
    {
        var definition = new RecurrenceDefinition(Frequency, AnchorMonth, AnchorDay);
        var dueOn = NextDueOn;
        while (dueOn < today && !(EndsOn.HasValue && dueOn > EndsOn.Value))
            dueOn = definition.GetNext(dueOn);

        return dueOn;
    }

    public void End(DateOnly endsOn) => EndsOn = endsOn;

    public void MoveToNextOccurrence()
    {
        NextDueOn = new RecurrenceDefinition(Frequency, AnchorMonth, AnchorDay)
            .GetNext(NextDueOn);
    }
}


