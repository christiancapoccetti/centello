using System.Data.Common;
using Centesimo.Application;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Centesimo.Infrastructure;

/// <summary>Tells the listener once a write is in the database for good: after SaveChanges outside a transaction, and
/// after the commit of one. Inside a transaction SaveChanges is not yet visible to another connection, and a
/// rollback says nothing. Without a listener (tests, design time) it does nothing.</summary>
internal sealed class StoreChangeInterceptor(IStoreChangeListener? listener = null)
    : DbTransactionInterceptor, ISaveChangesInterceptor
{
    public int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        SignalOutsideTransaction(eventData);
        return result;
    }

    public ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
        CancellationToken cancellationToken = default)
    {
        SignalOutsideTransaction(eventData);
        return new(result);
    }

    public override void TransactionCommitted(DbTransaction transaction, TransactionEndEventData eventData) =>
        listener?.StoreChanged();

    public override Task TransactionCommittedAsync(DbTransaction transaction, TransactionEndEventData eventData,
        CancellationToken cancellationToken = default)
    {
        listener?.StoreChanged();
        return Task.CompletedTask;
    }

    private void SignalOutsideTransaction(SaveChangesCompletedEventData eventData)
    {
        if (eventData.Context?.Database.CurrentTransaction is null)
            listener?.StoreChanged();
    }
}
