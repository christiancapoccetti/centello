using Centello.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App.ViewModels;

public sealed class RecurringPaymentsViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    private RecurringPaymentList? _list;
    private string _errorMessage = "";
    private bool _isEndedOpen;

    public RecurringPaymentList? List
    {
        get => _list;
        private set
        {
            if (!SetProperty(ref _list, value))
                return;

            OnPropertyChanged(nameof(HasPayments));
            OnPropertyChanged(nameof(IsEmpty));
            OnPropertyChanged(nameof(EndedCount));
        }
    }
    public bool HasPayments => List is { IsEmpty: false };
    public bool IsEmpty => List is { IsEmpty: true };
    public string EndedCount => (List?.Ended.Count ?? 0).ToString();
    /// <summary>The terminated payments start folded away: they are history, not something to act on.</summary>
    public bool IsEndedOpen { get => _isEndedOpen; private set => SetProperty(ref _isEndedOpen, value); }
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }

    public async Task Load()
    {
        ErrorMessage = "";
        Result<RecurringPaymentList> result;
        using (var scope = scopeFactory.CreateScope())
            result = await scope.ServiceProvider.GetRequiredService<RecurringPaymentListService>().Get();

        if (result.IsFailure)
            ErrorMessage = result.Error.Message;
        else
            List = result.Value;
    }

    public void ToggleEnded() => IsEndedOpen = !IsEndedOpen;
}
