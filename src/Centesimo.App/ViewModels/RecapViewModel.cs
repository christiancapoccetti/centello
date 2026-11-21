using Centesimo.Application;
using Centesimo.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centesimo.App.ViewModels;

/// <summary>The story of one period: the deck is read once per page, so coming back from an expense keeps the card
/// the person was on. The position lives in <see cref="RecapStory"/>; this only forwards.</summary>
public sealed class RecapViewModel(IServiceScopeFactory scopeFactory) : ObservableObject
{
    private Period? _period;
    private RecapStory? _story;
    private bool _isLoading;
    private string _errorMessage = "";

    public RecapStory? Story { get => _story; private set => SetProperty(ref _story, value); }
    public string ErrorMessage { get => _errorMessage; private set => SetProperty(ref _errorMessage, value); }
    public bool HasError => ErrorMessage.HasValue();
    public Period? Period => _period;
    public RecapCard? Current => Story?.Current;
    public string Title => Story?.Deck.Title ?? "";
    public string Badge => Story?.Deck.Badge ?? "";

    public void Initialize(Period period) => _period = period;

    public async Task Load()
    {
        if (Story is not null || _isLoading || _period is not { } period)
            return;

        _isLoading = true;
        Result<RecapDeck> result;
        using (var scope = scopeFactory.CreateScope())
            result = await scope.ServiceProvider.GetRequiredService<IRecapService>().Get(period);

        _isLoading = false;
        if (result.IsFailure)
        {
            ErrorMessage = result.Error.Message;
            OnPropertyChanged(nameof(HasError));
            return;
        }

        Story = new RecapStory(result.Value);
    }

    public bool Next() => Story?.Next() ?? false;

    public bool Previous() => Story?.Previous() ?? false;

    public void Restart() => Story?.Restart();
}
