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

    /// <summary>A photo on its way to OpenAI: the receipt on an indeterminate line, and the privacy note.</summary>
    public sealed record Reading(string Title, string Lead, string Glyph, string PrivacyNote) : PanelFace;

    /// <summary>A sentence and one way on. Annulla is always there.</summary>
    public sealed record Message(string Title, string Lead, PanelAction Action) : PanelFace;
}

/// <summary>Waiting: not yet recording. Listening: a tap sends. Sending: the recording is on its way.</summary>
public enum MicStage { Waiting, Listening, Sending }

public enum PanelAction { Retry, OpenSettings }

public static class PanelFaces
{
    private const string ListeningTitle = "Ti ascolto…";
    private const string ListeningLead = "Dì cosa hai speso, per esempio “ieri 12 euro di pizza”.";
    private const string SendLabel = "Tocca per inviare";
    private const string PreparingTitle = "Preparo la bozza…";
    private const string PreparingLead = "Poi la controlli prima di salvarla.";
    private const string SendingLabel = "Invio in corso";
    private static readonly TimeSpan Limit = TimeSpan.FromSeconds(SpeechRecordingLimits.MaximumDurationSeconds);

    /// <summary>Starting, Listening and Preparing of the voice panel -> Recording; NeedsApiKey -> Message(MissingKey,
    /// OpenSettings); Failed -> Message(error, Retry); Reading -> Reading from the source's title, reading line and
    /// privacy note. Everything the person does not see is Hidden: the photo before the shot, the camera in front, and
    /// DraftReady and Closed, where the panel is leaving.</summary>
    public static PanelFace Of(DraftSource source, PanelState state) => state switch
    {
        PanelState.Starting when source == DraftSource.Voice => Recording(TimeSpan.Zero, MicStage.Waiting),
        PanelState.Listening listening => Recording(listening.Heard, MicStage.Listening),
        PanelState.Preparing preparing => Recording(preparing.Heard, MicStage.Sending),
        PanelState.NeedsApiKey => new PanelFace.Message(source.SheetTitle, source.MissingKey, PanelAction.OpenSettings),
        PanelState.Failed failed => new PanelFace.Message(source.SheetTitle, failed.Error.Message, PanelAction.Retry),
        PanelState.Reading => new PanelFace.Reading(
            source.SheetTitle,
            source.ReadingLine,
            MaterialSymbols.ReceiptLong,
            source.PrivacyNote),
        _ => new PanelFace.Hidden()
    };

    private static PanelFace.Recording Recording(TimeSpan heard, MicStage mic) => mic == MicStage.Sending
        ? new PanelFace.Recording(PreparingTitle, PreparingLead, Clock(heard), Clock(Limit), Fill(heard), SendingLabel, mic)
        : new PanelFace.Recording(ListeningTitle, ListeningLead, Clock(heard), Clock(Limit), Fill(heard), SendLabel, mic);

    private static double Fill(TimeSpan heard) => Math.Clamp(heard / Limit, 0, 1);

    private static string Clock(TimeSpan time) => $"{(int)time.TotalMinutes}:{time.Seconds:00}";
}
