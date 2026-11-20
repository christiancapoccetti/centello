using Centesimo.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App.ViewModels;

public sealed class BackupViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    private bool _isBusy;

    /// <returns>Null when an export is already running: the tap is ignored.</returns>
    public async Task<Result<ExportedFile>?> ExportExpenses()
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
                return await scope.ServiceProvider.GetRequiredService<ExpenseExportService>().Export(directory);
            });
        }
        finally
        {
            _isBusy = false;
        }
    }
}
