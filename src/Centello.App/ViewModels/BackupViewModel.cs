using Centello.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App.ViewModels;

public sealed class BackupViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    private bool _isBusy;

    public Task<Result<ExportedFile>?> ExportExpenses() =>
        Run<ExpenseExportService>((service, directory) => service.Export(directory));

    public Task<Result<ExportedFile>?> CreateBackup() =>
        Run<IDataBackup>((service, directory) => service.Create(directory));

    /// <returns>Null when another file is being made: the tap is ignored.</returns>
    private async Task<Result<ExportedFile>?> Run<TService>(
        Func<TService, string, Task<Result<ExportedFile>>> create) where TService : notnull
    {
        if (_isBusy)
            return null;

        _isBusy = true;
        try
        {
            var directory = Path.Combine(FileSystem.CacheDirectory, "exports");
            return await Task.Run(async () =>
            {
                using var scope = scopeFactory.CreateScope();
                return await create(scope.ServiceProvider.GetRequiredService<TService>(), directory);
            });
        }
        finally
        {
            _isBusy = false;
        }
    }
}
