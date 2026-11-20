using Centesimo.Domain;

namespace Centesimo.Application;

public sealed record SaveRecurringPaymentRequest(Guid CategoryId, long AmountCents,
    RecurrenceFrequency Frequency, DateOnly NextDueOn, Guid? TagId = null,
    string Note = "", DateOnly? EndsOn = null);

/// <summary>What ending a payment now leaves behind: the date of the last expense and the sentence that says so.</summary>
public sealed record RecurringEndPreview(DateOnly LastExpenseOn, string Message);

public sealed class RecurringPaymentService(
    ICategoryRepository categories,
    ITagRepository tags,
    IExpenseRepository expenses,
    IRecurringPaymentRepository recurringPayments,
    IRecurringOccurrenceProcessor occurrenceProcessor,
    Func<DateOnly>? today = null)
{
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Today));

    public Task<Result<IReadOnlyList<RecurringPayment>>> GetAll(
        CancellationToken cancellationToken = default) => recurringPayments.GetAll(cancellationToken);

    public Task<Result<RecurringPayment?>> Get(Guid paymentId,
        CancellationToken cancellationToken = default) => recurringPayments.Get(paymentId, cancellationToken);

    public async Task<Result<RecurringPayment>> Create(SaveRecurringPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var validation = await Validate(request, cancellationToken);
        if (validation.IsFailure)
            return Result<RecurringPayment>.Failure(validation.Error);

        var payment = request.ToRecurringPayment(Guid.NewGuid());
        var saved = await recurringPayments.Add(payment, cancellationToken);
        return saved.IsFailure
            ? Result<RecurringPayment>.Failure(saved.Error)
            : Result<RecurringPayment>.Success(payment);
    }

    public async Task<Result> Update(Guid paymentId, SaveRecurringPaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        var found = await recurringPayments.Get(paymentId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.RecurringPaymentNotFound);

        var validation = await Validate(request, cancellationToken);
        if (validation.IsFailure)
            return validation;

        found.Value.Update(request.CategoryId, new Money(request.AmountCents), request.Frequency,
            request.NextDueOn, request.TagId, request.Note, request.EndsOn);
        return await recurringPayments.Update(found.Value, cancellationToken);
    }

    public Task<Result> Suspend(Guid paymentId, CancellationToken cancellationToken = default) =>
        Change(paymentId, payment => payment.Suspend(), cancellationToken);

    public Task<Result> Resume(Guid paymentId, CancellationToken cancellationToken = default) =>
        Change(paymentId, payment => payment.Resume(_today()), cancellationToken);

    public async Task<Result> End(Guid paymentId, DateOnly endsOn,
        CancellationToken cancellationToken = default)
    {
        var found = await recurringPayments.Get(paymentId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.RecurringPaymentNotFound);

        // Ending a paused payment resumes it first: the missed occurrences are skipped as on any resume, and the last
        // expense must be created, which a payment that stays paused never does.
        found.Value.Resume(_today());
        if (endsOn < found.Value.NextDueOn)
            return Result.Failure(ApplicationErrors.InvalidEndDate);

        found.Value.End(endsOn);
        return await recurringPayments.Update(found.Value, cancellationToken);
    }

    /// <summary>What "Termina" does: it ends the payment on its next due date (<c>End(paymentId, LastExpenseOn)</c>),
    /// so the occurrence due on that date is the last expense the payment creates. A paused payment skips the
    /// occurrences missed during the pause, so its last expense is the first one on or after today.</summary>
    public async Task<Result<RecurringEndPreview>> PreviewEnd(Guid paymentId,
        CancellationToken cancellationToken = default)
    {
        var found = await recurringPayments.Get(paymentId, cancellationToken);
        if (found.IsFailure)
            return Result<RecurringEndPreview>.Failure(found.Error);

        if (found.Value is not { } payment)
            return Result<RecurringEndPreview>.Failure(ApplicationErrors.RecurringPaymentNotFound);

        var lastExpenseOn = payment.IsSuspended ? payment.UpcomingDueOn(_today()) : payment.NextDueOn;
        if (payment.EndsOn < lastExpenseOn)
            return Result<RecurringEndPreview>.Failure(ApplicationErrors.RecurringPaymentAlreadyEnded);

        return Result<RecurringEndPreview>.Success(new RecurringEndPreview(lastExpenseOn,
            $"L'ultima spesa sarà quella {ItalianFormat.OfLongDate(lastExpenseOn)}, poi non ne verranno create altre."));
    }

    public async Task<Result<int>> ProcessDue(DateOnly today,
        CancellationToken cancellationToken = default)
    {
        var dueResult = await recurringPayments.GetDue(today, cancellationToken);
        if (dueResult.IsFailure)
            return Result<int>.Failure(dueResult.Error);

        var created = 0;
        foreach (var payment in dueResult.Value)
        {
            while (!payment.IsSuspended
                && payment.NextDueOn <= today
                && payment.NextDueOn <= payment.EndsOn.GetValueOrDefault(DateOnly.MaxValue))
            {
                var dueOn = payment.NextDueOn;
                var occurrence = new RecurrenceOccurrence(payment.RecurringPaymentId, dueOn);
                var request = new SaveExpenseRequest(payment.CategoryId, payment.Amount.Cents,
                    dueOn, payment.TagId, payment.Note);
                var expense = request.ToExpense(Guid.CreateVersion7(), payment.RecurringPaymentId);
                payment.MoveToNextOccurrence();
                var processed = await occurrenceProcessor.Process(
                    payment, expense, occurrence, cancellationToken);
                if (processed.IsFailure)
                    return Result<int>.Failure(processed.Error);

                if (processed.Value)
                    created++;
            }
        }

        return Result<int>.Success(created);
    }

    private async Task<Result> Change(Guid paymentId, Action<RecurringPayment> change,
        CancellationToken cancellationToken)
    {
        var found = await recurringPayments.Get(paymentId, cancellationToken);
        if (found.IsFailure)
            return Result.Failure(found.Error);

        if (found.Value is null)
            return Result.Failure(ApplicationErrors.RecurringPaymentNotFound);

        change(found.Value);
        return await recurringPayments.Update(found.Value, cancellationToken);
    }

    private async Task<Result> Validate(SaveRecurringPaymentRequest request,
        CancellationToken cancellationToken)
    {
        if (request.EndsOn < request.NextDueOn)
            return Result.Failure(ApplicationErrors.InvalidEndDate);

        var expenseValidation = new ExpenseService(categories, tags, expenses);
        var validationRequest = new SaveExpenseRequest(request.CategoryId, request.AmountCents,
            request.NextDueOn, request.TagId, request.Note);
        return await expenseValidation.ValidateForRecurringPayment(validationRequest, cancellationToken);
    }
}
