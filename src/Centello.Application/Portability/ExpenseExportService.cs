using System.Globalization;

namespace Centello.Application;

public sealed record ExportedFile(string FilePath, string ContentType);

public sealed class ExpenseExportService(IExpenseRepository expenses, ICategoryRepository categories,
    ITagRepository tags, Func<DateOnly>? today = null)
{
    private const string CsvContentType = "text/csv";
    private readonly Func<DateOnly> _today = today ?? (() => DateOnly.FromDateTime(DateTime.Now));

    public async Task<Result<ExportedFile>> Export(string directory, CancellationToken cancellationToken = default)
    {
        // The repositories share one scoped DbContext, so the reads cannot overlap.
        var expenseResult = await expenses.GetBetween(DateOnly.MinValue, DateOnly.MaxValue, cancellationToken);
        if (expenseResult.IsFailure)
            return Result<ExportedFile>.Failure(PortabilityErrors.ExportFailed);

        var categoryResult = await categories.GetAll(cancellationToken);
        if (categoryResult.IsFailure)
            return Result<ExportedFile>.Failure(PortabilityErrors.ExportFailed);

        var tagResult = await tags.GetAll(cancellationToken);
        if (tagResult.IsFailure)
            return Result<ExportedFile>.Failure(PortabilityErrors.ExportFailed);

        var name = $"centello-spese-{_today().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}.csv";
        var path = Path.Combine(directory, name);
        try
        {
            Directory.CreateDirectory(directory);
            await File.WriteAllBytesAsync(
                path, ExpenseCsv.Write(expenseResult.Value, categoryResult.Value, tagResult.Value), cancellationToken);
        }
        catch (IOException)
        {
            return Result<ExportedFile>.Failure(PortabilityErrors.ExportFailed);
        }

        return Result<ExportedFile>.Success(new ExportedFile(path, CsvContentType));
    }
}
