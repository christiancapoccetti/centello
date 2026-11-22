using Centello.Application;
using Microsoft.EntityFrameworkCore;

namespace Centello.Infrastructure;

public abstract class RepositoryBase
{
    protected readonly CentelloDbContext Context;

    protected RepositoryBase(CentelloDbContext context) =>
        Context = context ?? throw new ArgumentNullException(nameof(context));

    protected async Task<Result<TValue>> UseContext<TValue>(
        Func<CentelloDbContext, CancellationToken, Task<TValue>> query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Result<TValue>.Success(await query(Context, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return Result<TValue>.Failure(InfrastructureErrors.PersistenceFailure);
        }
        catch (Exception)
        {
            return Result<TValue>.Failure(InfrastructureErrors.Unexpected);
        }
    }

    protected async Task<Result> SaveContext(
        Action<CentelloDbContext> mutation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            mutation(Context);
            await Context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return Result.Failure(InfrastructureErrors.PersistenceFailure);
        }
        catch (Exception)
        {
            return Result.Failure(InfrastructureErrors.Unexpected);
        }
        finally
        {
            Context.ChangeTracker.Clear();
        }
    }

    protected async Task<Result> SaveContext(
        Func<CentelloDbContext, CancellationToken, Task> mutation,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await mutation(Context, cancellationToken);
            await Context.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return Result.Failure(InfrastructureErrors.PersistenceFailure);
        }
        catch (Exception)
        {
            return Result.Failure(InfrastructureErrors.Unexpected);
        }
        finally
        {
            Context.ChangeTracker.Clear();
        }
    }

    protected async Task<Result<TValue>> SaveContextInTransaction<TValue>(
        Func<CentelloDbContext, CancellationToken, Task<TValue>> mutation,
        CancellationToken cancellationToken = default, Func<TValue, bool>? shouldSave = null)
    {
        try
        {
            // SQLite reserves the writer before the mutation reads the tracked rows.
            await using var transaction = await Context.Database
                .BeginTransactionAsync(cancellationToken);
            var value = await mutation(Context, cancellationToken);
            if (shouldSave is not null && !shouldSave(value))
                return Result<TValue>.Success(value);

            await Context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return Result<TValue>.Success(value);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (DbUpdateException)
        {
            return Result<TValue>.Failure(InfrastructureErrors.PersistenceFailure);
        }
        catch (Exception)
        {
            return Result<TValue>.Failure(InfrastructureErrors.Unexpected);
        }
        finally
        {
            Context.ChangeTracker.Clear();
        }
    }
}
