using Centello.Domain;

namespace Centello.Application;

public sealed record SaveRecurringPaymentRequest(Guid CategoryId, long AmountCents,
    RecurrenceFrequency Frequency, DateOnly NextDueOn, Guid? TagId = null,
    string Note = "", DateOnly? EndsOn = null, DateOnly? OriginalNextDueOn = null);

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
        var validation = await Validate(request, cancellationToken);
        if (validation.IsFailure)
            return validation;

        return await recurringPayments.Update(paymentId, payment =>
        {
            // Keep any advancement the automation committed while the editor was open.
            var nextDueOn = request.OriginalNextDueOn == request.NextDueOn
                ? payment.NextDueOn
                : request.NextDueOn;
            if (request.EndsOn < nextDueOn)
                return Result.Failure(ApplicationErrors.InvalidEndDate);

            var nextDueOnWasEdited = request.OriginalNextDueOn is { } originalNextDueOn
                && originalNextDueOn != request.NextDueOn;
            payment.Update(request.CategoryId, new Money(request.AmountCents), request.Frequency,
                nextDueOn, request.TagId, request.Note, request.EndsOn, nextDueOnWasEdited);
            return Result.Success();
        }, cancellationToken);
    }

    public Task<Result> Suspend(Guid paymentId, CancellationToken cancellationToken = default) =>
        Change(paymentId, payment => payment.Suspend(), cancellationToken);

    public Task<Result> Resume(Guid paymentId, CancellationToken cancellationToken = default) =>
        Change(paymentId, payment => payment.Resume(_today()), cancellationToken);

    public Task<Result> End(Guid paymentId, DateOnly endsOn,
        CancellationToken cancellationToken = default) =>
        recurringPayments.Update(paymentId, payment =>
        {
            var today = _today();
            var lastExpenseOn = payment.IsSuspended ? payment.UpcomingDueOn(today) : payment.NextDueOn;
            if (endsOn < lastExpenseOn)
                return Result.Failure(ApplicationErrors.InvalidEndDate);

            // A paused payment must skip missed dates and become active for its final expense.
            payment.Resume(today);
            payment.End(endsOn);
            return Result.Success();
        }, cancellationToken);

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
            while (true)
            {
                var processed = await occurrenceProcessor.Process(
                    payment.RecurringPaymentId, today, cancellationToken);
                if (processed.IsFailure)
                    return Result<int>.Failure(processed.Error);

                if (processed.Value == RecurringOccurrenceOutcome.NotDue)
                    break;

                if (processed.Value == RecurringOccurrenceOutcome.Created)
                    created++;
            }
        }

        return Result<int>.Success(created);
    }

    private Task<Result> Change(Guid paymentId, Action<RecurringPayment> change,
        CancellationToken cancellationToken) =>
        recurringPayments.Update(paymentId, payment =>
        {
            change(payment);
            return Result.Success();
        }, cancellationToken);

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
