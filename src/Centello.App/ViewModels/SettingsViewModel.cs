using Centello.Application;
using Centello.Domain;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App.ViewModels;

public sealed class SettingsViewModel(IServiceScopeFactory scopeFactory, OpenAiSettings openAiSettings,
    RecapNoticeAutomation recapNotices, IRecapNoticeSettings recapNoticeSettings, IRecapNoticeBoard recapNoticeBoard,
    INotificationPermission notificationPermission) : ObservableObject
{
    private const string NewKeyLead = "Detta una spesa con parole tue, per esempio “ieri 12 euro di pizza”, o fotografa uno scontrino o una fattura, o scegli un PDF. Serve una tua chiave API OpenAI.";
    private const string ReplacementLead = "La nuova chiave prende il posto di quella salvata.";

    private string _recurringSummary = "";
    private bool _hasApiKey;
    private bool _isReplacing;
    private string _apiKeyInput = "";
    private string _apiKeyError = "";
    private bool _recapNoticeOn;
    private string _recapNoticeDetail = "";
    private bool _recapNoticeBlocked;

    public bool RecapNoticeOn { get => _recapNoticeOn; private set => SetProperty(ref _recapNoticeOn, value); }
    public string RecapNoticeDetail { get => _recapNoticeDetail; private set => SetProperty(ref _recapNoticeDetail, value); }
    /// <summary>The person wants the notice but Android does not let the app notify: only the system settings can fix it.</summary>
    public bool RecapNoticeBlocked { get => _recapNoticeBlocked; private set => SetProperty(ref _recapNoticeBlocked, value); }
    public string RecurringSummary { get => _recurringSummary; private set => SetProperty(ref _recurringSummary, value); }
    public string VersionText => $"Versione {AppInfo.Current.VersionString}";
    public bool HasApiKey
    {
        get => _hasApiKey;
        private set
        {
            if (SetProperty(ref _hasApiKey, value))
                NotifyKeyStateChanged();
        }
    }
    /// <summary>The person is typing a key to put in place of the one saved.</summary>
    public bool IsReplacing
    {
        get => _isReplacing;
        private set
        {
            if (SetProperty(ref _isReplacing, value))
                NotifyKeyStateChanged();
        }
    }
    public bool ShowKeyStatus => HasApiKey && !IsReplacing;
    public bool ShowKeyForm => !ShowKeyStatus;
    public string KeyLead => IsReplacing ? ReplacementLead : NewKeyLead;
    public string ApiKeyInput
    {
        get => _apiKeyInput;
        set
        {
            if (!SetProperty(ref _apiKeyInput, value))
                return;

            ApiKeyError = "";
        }
    }
    public string ApiKeyError { get => _apiKeyError; private set => SetProperty(ref _apiKeyError, value); }

    /// <summary>Off cancels the alarm. On asks Android for the permission when it is missing and is kept even when refused,
    /// so the detail can send the person to the system settings and the notice starts by itself once they allow it.</summary>
    public async Task SetRecapNotice(bool on)
    {
        if (on && !recapNoticeBoard.CanPost)
            await notificationPermission.Request();

        await recapNotices.Choose(on ? RecapNoticeState.On : RecapNoticeState.Off);
        RefreshRecapNotice();
    }

    public async Task Load()
    {
        RefreshRecapNotice();
        HasApiKey = await openAiSettings.HasApiKey();
        using var scope = scopeFactory.CreateScope();
        var list = await scope.ServiceProvider.GetRequiredService<RecurringPaymentListService>().Get();
        // The row keeps its last words when the list cannot be read: the payments page says what went wrong.
        if (list.IsSuccess)
            RecurringSummary = list.Value.ActiveSummary;
    }

    public void StartReplacing() => IsReplacing = true;

    public void StopReplacing()
    {
        IsReplacing = false;
        ClearInput();
    }

    /// <summary>False, with the reason on screen, when there is nothing to save: the consent is asked only for a key.</summary>
    public bool HasKeyToSave()
    {
        if (ApiKeyInput.HasValue())
            return true;

        ApiKeyError = DraftErrors.EmptyApiKey.Message;
        return false;
    }

    /// <returns>True when the key is saved. Without consent nothing happens and nothing is said.</returns>
    public async Task<bool> SaveApiKey(bool consentAccepted)
    {
        if (!consentAccepted)
            return false;

        var result = await openAiSettings.SaveApiKey(ApiKeyInput, consentAccepted);
        if (result.IsFailure)
        {
            ApiKeyError = result.Error.Message;
            return false;
        }

        ClearInput();
        IsReplacing = false;
        HasApiKey = true;
        return true;
    }

    public void RemoveApiKey()
    {
        openAiSettings.ClearApiKey();
        ClearInput();
        HasApiKey = false;
    }

    public void RefreshRecapNotice()
    {
        var state = recapNoticeSettings.State;
        var canPost = recapNoticeBoard.CanPost;
        RecapNoticeOn = RecapNoticeAutomation.IsEnabled(state);
        RecapNoticeBlocked = RecapNoticeOn && !canPost;
        RecapNoticeDetail = RecapNoticeAutomation.Describe(state, canPost);
    }

    private void ClearInput()
    {
        ApiKeyInput = "";
        ApiKeyError = "";
    }

    private void NotifyKeyStateChanged()
    {
        OnPropertyChanged(nameof(ShowKeyStatus));
        OnPropertyChanged(nameof(ShowKeyForm));
        OnPropertyChanged(nameof(KeyLead));
    }
}
