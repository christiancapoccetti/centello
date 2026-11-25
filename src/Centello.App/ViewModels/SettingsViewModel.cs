using Centello.Application;
using Microsoft.Extensions.DependencyInjection;

namespace Centello.App.ViewModels;

public sealed class SettingsViewModel(IServiceScopeFactory scopeFactory, DraftConsent draftConsent, AiAccess aiAccess,
    RecapNoticeAutomation recapNotices, IRecapNoticeSettings recapNoticeSettings, IRecapNoticeBoard recapNoticeBoard,
    INotificationPermission notificationPermission, FeedbackForm feedbackForm) : ObservableObject
{
    private string _recurringSummary = "";
    private bool _draftsEnabled;
    private string _aiInviteTitle = "Attiva un invito";
    private string _aiInviteDetail = "";
    private bool _recapNoticeOn;
    private string _recapNoticeDetail = "";
    private bool _recapNoticeBlocked;

    public bool RecapNoticeOn { get => _recapNoticeOn; private set => SetProperty(ref _recapNoticeOn, value); }
    public string RecapNoticeDetail { get => _recapNoticeDetail; private set => SetProperty(ref _recapNoticeDetail, value); }
    /// <summary>The person wants the notice but Android does not let the app notify: only the system settings can fix it.</summary>
    public bool RecapNoticeBlocked { get => _recapNoticeBlocked; private set => SetProperty(ref _recapNoticeBlocked, value); }
    public string RecurringSummary { get => _recurringSummary; private set => SetProperty(ref _recurringSummary, value); }
    public string VersionText => $"Versione {AppInfo.Current.VersionString}";
    public Uri? FeedbackUrl => feedbackForm.Url;
    public bool HasFeedbackForm => FeedbackUrl is not null;
    public void RefreshFeedback() => OnPropertyChanged(nameof(HasFeedbackForm));

    public bool DraftsEnabled { get => _draftsEnabled; private set => SetProperty(ref _draftsEnabled, value); }
    public string AiInviteTitle { get => _aiInviteTitle; private set => SetProperty(ref _aiInviteTitle, value); }
    public string AiInviteDetail { get => _aiInviteDetail; private set => SetProperty(ref _aiInviteDetail, value); }

    public void SetDraftsEnabled(bool enabled)
    {
        draftConsent.SetEnabled(enabled);
        DraftsEnabled = draftConsent.IsEnabled;
    }

    /// <summary>Off cancels the alarm. On asks Android for the permission when it is missing and is kept even when refused,
    /// so the detail can send the person to the system settings and the notice starts by itself once they allow it.</summary>
    public async Task SetRecapNotice(bool on)
    {
        if (on && !recapNoticeBoard.CanPost)
            await notificationPermission.Request();

        await recapNotices.Choose(on ? RecapNoticeState.On : RecapNoticeState.Off);
        RefreshRecapNotice();
    }

    public Task Load()
    {
        RefreshRecapNotice();
        DraftsEnabled = draftConsent.IsEnabled;
        RefreshAiAccess();
        return RefreshPayments();
    }

    private async Task RefreshPayments()
    {
        using var scope = scopeFactory.CreateScope();
        var list = await scope.ServiceProvider.GetRequiredService<RecurringPaymentListService>().Get();
        // The row keeps its last words when the list cannot be read: the payments page says what went wrong.
        if (list.IsSuccess)
            RecurringSummary = list.Value.ActiveSummary;
    }

    public void RefreshAiAccess()
    {
        if (aiAccess.HasInvitation)
        {
            AiInviteTitle = "Invito AI";
            AiInviteDetail = aiAccess.IsInvitationVerified ? "Invito attivo" : DraftErrors.InviteVerificationUnavailable.Message;
            return;
        }

        AiInviteTitle = "Attiva un invito";
        var trials = aiAccess.TrialsRemaining switch
        {
            0 => "Prove gratuite esaurite",
            1 => "1 prova gratuita rimasta",
            var remaining => $"{remaining} prove gratuite rimaste"
        };
        AiInviteDetail = aiAccess.WasInvitationRevoked ? $"Invito revocato · {trials}" : trials;
    }

    public void RefreshRecapNotice()
    {
        var state = recapNoticeSettings.State;
        var canPost = recapNoticeBoard.CanPost;
        RecapNoticeOn = RecapNoticeAutomation.IsEnabled(state);
        RecapNoticeBlocked = RecapNoticeOn && !canPost;
        RecapNoticeDetail = RecapNoticeAutomation.Describe(state, canPost);
    }
}
