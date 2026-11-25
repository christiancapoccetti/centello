using Centello.Application;
using Centello.Domain;

namespace Centello.App.ViewModels;

public sealed class AiInviteViewModel(AiAccess access) : ObservableObject
{
    private string _code = "";
    private bool _isActive;
    private bool _isBusy;
    private Error? _error;

    public string InstallationId { get; } = access.InstallationId;
    public bool HasInstallationId => InstallationId.HasValue();
    public string InstallationIdError => HasInstallationId ? "" : "Non riesco a leggere l’ID di questo telefono.";
    public bool CanShare => HasInstallationId && !IsBusy;
    public string Title => IsActive ? "Invito AI" : "Attiva un invito";
    public bool IsActive => _isActive;
    public bool ShowForm => !IsActive;
    public bool IsBusy => _isBusy;
    public bool CanEdit => !IsBusy;
    public bool CanActivate => ShowForm && !IsBusy && (Code.HasValue() || ShouldRetrySavedInvite);
    public string ActivateText => IsBusy ? "Verifica in corso…"
        : _error == DraftErrors.InviteVerificationUnavailable ? "Riprova" : "Attiva invito";
    public bool HasError => _error is not null;
    public bool HasInputError => _error == DraftErrors.InvalidInvite || _error == DraftErrors.InviteDeviceMismatch;
    public string ErrorMessage => _error switch
    {
        null => "",
        var error when error == DraftErrors.InvalidInvite => "Codice non valido. Copia il codice completo dal messaggio ricevuto.",
        var error when error == DraftErrors.InviteDeviceMismatch => "Questo codice è per un altro telefono. Condividi il tuo ID per riceverne uno nuovo.",
        var error when error == DraftErrors.InviteRevoked => "Questo invito è stato revocato. Puoi chiedere un nuovo codice.",
        var error when error == DraftErrors.InviteVerificationUnavailable => "Non riesco a verificare l’invito. Controlla la connessione e riprova. La verifica non consuma prove.",
        var error => error.Message
    };

    private bool ShouldRetrySavedInvite => _error == DraftErrors.InviteVerificationUnavailable && access.HasInvitation;

    public string Code
    {
        get => _code;
        set
        {
            if (SetProperty(ref _code, value ?? ""))
                NotifyActions();
        }
    }

    public async Task Refresh(CancellationToken cancellationToken)
    {
        if (IsBusy)
            return;

        if (!access.HasInvitation || access.IsInvitationVerified)
        {
            UpdateAccess();
            return;
        }

        SetBusy(true);
        try
        {
            var check = await access.Check(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;

            SetError(access.WasInvitationRevoked ? DraftErrors.InviteRevoked : check.IsFailure ? check.Error : null);
        }
        finally
        {
            SetBusy(false);
            if (!cancellationToken.IsCancellationRequested)
                UpdateAccess();
        }
    }

    public void UpdateAccess()
    {
        if (IsBusy)
            return;

        SetActive(access.IsInvitationVerified);
        if (IsActive)
        {
            SetError(null);
            return;
        }

        if (access.WasInvitationRevoked && (!HasError || _error == DraftErrors.InviteVerificationUnavailable))
            SetError(DraftErrors.InviteRevoked);
        else if (access.HasInvitation && !HasError)
            SetError(DraftErrors.InviteVerificationUnavailable);
    }

    public async Task Activate(CancellationToken cancellationToken)
    {
        if (!CanActivate)
            return;

        if (Code.IsEmpty())
        {
            await Refresh(cancellationToken);
            return;
        }

        SetBusy(true);
        try
        {
            var result = await access.Redeem(Code, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;

            if (result.IsFailure)
            {
                SetError(result.Error);
                return;
            }

            Code = "";
            SetError(null);
        }
        finally
        {
            SetBusy(false);
            if (!cancellationToken.IsCancellationRequested)
                UpdateAccess();
        }
    }

    private void SetActive(bool active)
    {
        if (!SetProperty(ref _isActive, active, nameof(IsActive)))
            return;

        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(ShowForm));
        OnPropertyChanged(nameof(CanActivate));
    }

    private void SetBusy(bool busy)
    {
        if (!SetProperty(ref _isBusy, busy, nameof(IsBusy)))
            return;

        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(CanShare));
        NotifyActions();
    }

    private void SetError(Error? error)
    {
        if (!SetProperty(ref _error, error, nameof(HasError)))
            return;

        OnPropertyChanged(nameof(HasInputError));
        OnPropertyChanged(nameof(ErrorMessage));
        NotifyActions();
    }

    private void NotifyActions()
    {
        OnPropertyChanged(nameof(CanActivate));
        OnPropertyChanged(nameof(ActivateText));
    }
}
