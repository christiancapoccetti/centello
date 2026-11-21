using Centesimo.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.Application.Tests;

internal sealed class FakeCategoryRepository : ICategoryRepository
{
    public List<Category> Items { get; } = [];
    public Error? Failure { get; set; }
    public Error? AddFailure { get; set; }
    public List<Tag> AddedTags { get; } = [];
    public Task<Result> Add(Category value, IReadOnlyList<Tag> tags, CancellationToken token = default)
    {
        if (AddFailure is not null)
            return Task.FromResult(Result.Failure(AddFailure));

        Items.Add(value);
        AddedTags.AddRange(tags);
        return Task.FromResult(Operation());
    }
    public Task<Result<Category?>> Get(Guid id, CancellationToken token = default) => Task.FromResult(Failure is null ? Result<Category?>.Success(Items.SingleOrDefault(x => x.CategoryId == id)) : Result<Category?>.Failure(Failure));
    public Task<Result<IReadOnlyList<Category>>> GetAll(CancellationToken token = default) => Task.FromResult(Failure is null ? Result<IReadOnlyList<Category>>.Success(Items) : Result<IReadOnlyList<Category>>.Failure(Failure));
    public Task<Result> Update(Category value, CancellationToken token = default) => Task.FromResult(Operation());
    private Result Operation() => Failure is null ? Result.Success() : Result.Failure(Failure);
}

internal sealed class FakeTagRepository : ITagRepository
{
    public List<Tag> Items { get; } = [];
    public Error? Failure { get; set; }
    public Task<Result> Add(Tag value, CancellationToken token = default) { Items.Add(value); return Task.FromResult(Result.Success()); }
    public Task<Result<Tag?>> Get(Guid id, CancellationToken token = default) => Task.FromResult(Result<Tag?>.Success(Items.SingleOrDefault(x => x.TagId == id)));
    public Task<Result<IReadOnlyList<Tag>>> GetAll(CancellationToken token = default) => Task.FromResult(Failure is null ? Result<IReadOnlyList<Tag>>.Success(Items) : Result<IReadOnlyList<Tag>>.Failure(Failure));
    public Task<Result<IReadOnlyList<Tag>>> GetByCategory(Guid id, CancellationToken token = default) => Task.FromResult(Failure is null ? Result<IReadOnlyList<Tag>>.Success(Items.Where(x => x.CategoryId == id).ToList()) : Result<IReadOnlyList<Tag>>.Failure(Failure));
    public Task<Result> Update(Tag value, CancellationToken token = default) => Task.FromResult(Result.Success());
}

internal sealed class FakeExpenseRepository : IExpenseRepository
{
    public List<Expense> Items { get; } = [];
    public Error? Failure { get; set; }
    public Task<Result> Add(Expense value, CancellationToken token = default) { Items.Add(value); return Task.FromResult(Result.Success()); }
    public Task<Result<Expense?>> Get(Guid id, CancellationToken token = default) => Task.FromResult(Result<Expense?>.Success(Items.SingleOrDefault(x => x.ExpenseId == id)));
    public Task<Result<IReadOnlyList<Expense>>> GetByCategoryBetween(Guid categoryId, DateOnly from, DateOnly to, CancellationToken token = default) => Task.FromResult(Failure is null ? Result<IReadOnlyList<Expense>>.Success(Items.Where(x => x.CategoryId == categoryId && x.OccurredOn >= from && x.OccurredOn <= to).ToList()) : Result<IReadOnlyList<Expense>>.Failure(Failure));
    public Task<Result<IReadOnlyList<Expense>>> GetBetween(DateOnly from, DateOnly to, CancellationToken token = default) => Task.FromResult(Failure is null ? Result<IReadOnlyList<Expense>>.Success(Items.Where(x => x.OccurredOn >= from && x.OccurredOn <= to).ToList()) : Result<IReadOnlyList<Expense>>.Failure(Failure));
    public Task<Result> Update(Expense value, CancellationToken token = default) { Items.RemoveAll(x => x.ExpenseId == value.ExpenseId); Items.Add(value); return Task.FromResult(Result.Success()); }
    public Task<Result> Delete(Guid id, CancellationToken token = default) { Items.RemoveAll(x => x.ExpenseId == id); return Task.FromResult(Result.Success()); }
}

internal sealed class FakeRecurringPaymentRepository : IRecurringPaymentRepository
{
    public List<RecurringPayment> Items { get; } = [];
    public Error? Failure { get; set; }
    public Task<Result> Add(RecurringPayment value, CancellationToken token = default) { Items.Add(value); return Task.FromResult(Result.Success()); }
    public Task<Result<RecurringPayment?>> Get(Guid id, CancellationToken token = default) => Task.FromResult(Failure is null ? Result<RecurringPayment?>.Success(Items.SingleOrDefault(x => x.RecurringPaymentId == id)) : Result<RecurringPayment?>.Failure(Failure));
    public Task<Result<IReadOnlyList<RecurringPayment>>> GetAll(CancellationToken token = default) => Task.FromResult(Failure is null ? Result<IReadOnlyList<RecurringPayment>>.Success(Items.OrderBy(x => x.NextDueOn).ToList()) : Result<IReadOnlyList<RecurringPayment>>.Failure(Failure));
    public Task<Result<IReadOnlyList<RecurringPayment>>> GetDue(DateOnly throughDate, CancellationToken token = default) => Task.FromResult(Result<IReadOnlyList<RecurringPayment>>.Success(Items.Where(x => !x.IsSuspended && x.NextDueOn <= throughDate && (!x.EndsOn.HasValue || x.NextDueOn <= x.EndsOn.Value)).ToList()));
    public Task<Result> Update(RecurringPayment value, CancellationToken token = default) => Task.FromResult(Result.Success());
}

internal sealed class FakeRecurringOccurrenceProcessor(FakeExpenseRepository expenses)
    : IRecurringOccurrenceProcessor
{
    private readonly HashSet<(Guid, DateOnly)> _occurrences = [];
    public Task<Result<bool>> Process(RecurringPayment payment, Expense expense,
        RecurrenceOccurrence occurrence, CancellationToken token = default)
    {
        var added = _occurrences.Add((occurrence.RecurringPaymentId, occurrence.DueOn));
        if (added)
            expenses.Items.Add(expense);

        return Task.FromResult(Result<bool>.Success(added));
    }
}


internal sealed class FakeServiceProvider(params object[] services) : IServiceProvider
{
    public object? GetService(Type serviceType) => services.FirstOrDefault(serviceType.IsInstanceOfType);
}

internal sealed class FakeScopeFactory(IServiceProvider provider) : IServiceScopeFactory
{
    public IServiceScope CreateScope() => new Scope(provider);

    private sealed class Scope(IServiceProvider provider) : IServiceScope
    {
        public IServiceProvider ServiceProvider => provider;

        public void Dispose()
        {
        }
    }
}

internal sealed class FakeRecapService : IRecapService
{
    public HashSet<Period> Available { get; } = [];
    public Error? Failure { get; set; }

    public Task<Result<RecapDeck>> Get(Period period, CancellationToken cancellationToken = default) =>
        Task.FromResult(Result<RecapDeck>.Failure(RecapErrors.NotAvailable));

    public Task<Result<bool>> IsAvailable(Period period, CancellationToken cancellationToken = default) =>
        Task.FromResult(Failure is null
            ? Result<bool>.Success(Available.Contains(period))
            : Result<bool>.Failure(Failure));
}

internal sealed class FakeRecapNoticeBoard : IRecapNoticeBoard
{
    public bool CanPost { get; set; } = true;
    public bool PostSucceeds { get; set; } = true;
    public List<RecapNotice> Posted { get; } = [];
    public List<DateTime> Armed { get; } = [];
    public int Disarmed { get; private set; }

    public Task<bool> Post(RecapNotice notice)
    {
        if (!PostSucceeds)
            return Task.FromResult(false);

        Posted.Add(notice);
        return Task.FromResult(true);
    }

    public Task ArmAt(DateTime local)
    {
        Armed.Add(local);
        return Task.CompletedTask;
    }

    public Task Disarm()
    {
        Disarmed++;
        return Task.CompletedTask;
    }
}

internal sealed class FakeRecapNoticeSettings : IRecapNoticeSettings
{
    public RecapNoticeState State { get; set; } = RecapNoticeState.On;
    public string LastHandledKey { get; set; } = "";
}
