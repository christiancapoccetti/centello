namespace Centello.Application;

/// <summary>What the panel's sheet shows. Derived from the capture's state every time, never stored.</summary>
public abstract record PanelFace
{
    private PanelFace()
    {
    }

    /// <summary>Nothing over the launcher: the photo panel before the shot, while a dialog or the camera is in front.</summary>
    public sealed record Hidden : PanelFace;

    /// <summary>The voice panel's 20 s line and microphone.</summary>
    public sealed record Recording(
        string Title,
        string Lead,
        string Elapsed,
        string Remaining,
        double Fill,
        string MicLabel,
        MicStage Mic) : PanelFace;

    public sealed record Reading(string Title, string Lead, string Glyph, string PrivacyNote) : PanelFace;

    /// <summary>A sentence and one way on. Annulla is always there.</summary>
    public sealed record Message(string Title, string Lead, PanelAction Action) : PanelFace;

    public sealed record AccessBlocked(string Title, string Lead, string SurveyUrl = "", bool CanRedeem = false,
        bool IsTrialExhausted = false) : PanelFace
    {
        public bool CanOpenSurvey => SurveyUrl.Length > 0;
        public bool ShowsClose => !CanRedeem;
    }
}

/// <summary>Waiting: not yet recording. Listening: a tap sends. Sending: the recording is on its way.</summary>
public enum MicStage { Waiting, Listening, Sending }

public enum PanelAction { Retry, OpenSettings, OpenManual, OpenInvitation, OpenSurvey }

public static class PanelFaces
{
    private const string ListeningTitle = "Ti ascolto…";
    private const string ListeningLead = "Dì cosa hai speso, per esempio “ieri 12 euro di pizza”.";
    private const string SendLabel = "Tocca per inviare";
    private const string PreparingTitle = "Preparo la bozza…";
    private const string PreparingLead = "Poi la controlli prima di salvarla.";
    private const string SendingLabel = "Invio in corso";
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(SpeechRecordingLimits.MaximumDurationSeconds);

    public static PanelFace Of(DraftSource source, PanelState state, string surveyUrl = "") => state switch
    {
        PanelState.Starting when source == DraftSource.Voice => Recording(TimeSpan.Zero, MicStage.Waiting),
        PanelState.Listening listening => Recording(listening.Heard, MicStage.Listening),
        PanelState.Preparing preparing => Recording(preparing.Heard, MicStage.Sending),
        PanelState.NeedsConsent => new PanelFace.Message(source.SheetTitle, DraftErrors.ConsentRequired.Message, PanelAction.OpenSettings),
        PanelState.Failed failed => AccessFailure(failed.Error, surveyUrl)
            ?? (PanelFace)new PanelFace.Message(source.SheetTitle, failed.Error.Message, PanelAction.Retry),
        PanelState.Reading => new PanelFace.Reading(
            source.SheetTitle,
            source.ReadingLine,
            MaterialSymbols.ReceiptLong,
            source.PrivacyNote),
        _ => new PanelFace.Hidden()
    };

    public static PanelFace.AccessBlocked? AccessFailure(Error error, string surveyUrl)
    {
        if (error == DraftErrors.TrialExhausted)
        {
            var explanation = "Creare una bozza da voce, foto o PDF ha un costo.";
            if (surveyUrl.Length > 0)
                explanation += " Stiamo valutando un piano mensile: ti interesserebbe?"
                    + " Con due domande puoi dirci se lo useresti e quanto spenderesti.";

            return new PanelFace.AccessBlocked(
                "Hai usato le 5 prove gratuite",
                explanation + "\n\nPuoi sempre inserire le spese a mano gratuitamente.",
                surveyUrl,
                CanRedeem: true,
                IsTrialExhausted: true);
        }

        if (error == DraftErrors.InviteVerificationUnavailable)
            return new PanelFace.AccessBlocked("L’AI non è disponibile", error.Message);

        if (error == DraftErrors.InviteRevoked)
            return new PanelFace.AccessBlocked("L’invito è stato revocato", error.Message, CanRedeem: true);

        if (error == DraftErrors.NoInternet || error == DraftErrors.AccessDenied || error == DraftErrors.RateLimited
            || error == DraftErrors.TimedOut || error == DraftErrors.Unavailable || error == DraftErrors.RequestFailed)
            return new PanelFace.AccessBlocked("L’AI non è disponibile", "Puoi inserire la spesa a mano. Questa richiesta non consuma una prova.");

        return null;
    }

    private static PanelFace.Recording Recording(TimeSpan heard, MicStage mic) => mic == MicStage.Sending
        ? new PanelFace.Recording(PreparingTitle, PreparingLead, Clock(heard), Clock(Limit), Fill(heard), SendingLabel, mic)
        : new PanelFace.Recording(ListeningTitle, ListeningLead, Clock(heard), Clock(Limit), Fill(heard), SendLabel, mic);

    private static double Fill(TimeSpan heard) => Math.Clamp(heard / Limit, 0, 1);

    private static string Clock(TimeSpan time) => $"{(int)time.TotalMinutes}:{time.Seconds:00}";
}
