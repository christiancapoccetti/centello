using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>
/// What the home shows while a draft is captured, for voice and photo alike: the photo menu, the listening toast and
/// the reading sheet. One value means one capture at a time. Nothing starts unless the capture is idle, and the sheet
/// always speaks for the source that opened it. A transition that does not apply to the current phase returns the
/// same value, so a late, repeated or crossed event changes nothing.
/// </summary>
public sealed record DraftCapture
{
    public static DraftCapture Idle { get; } = new(Phase.Idle, null, "", "");

    private readonly Phase _phase;

    private DraftCapture(Phase phase, DraftSource? source, string errorMessage, string heard)
    {
        _phase = phase;
        Source = source;
        ErrorMessage = errorMessage;
        Heard = heard;
    }

    /// <summary>The source the menu, the toast or the sheet speaks for. Null only while idle.</summary>
    public DraftSource? Source { get; }
    public string ErrorMessage { get; }
    /// <summary>What the voice heard before the reading failed, shown above the error.</summary>
    public string Heard { get; }

    public bool IsIdle => _phase == Phase.Idle;
    public bool IsChoosingPhoto => _phase == Phase.Choosing;
    public bool IsListening => _phase == Phase.Listening;
    public bool IsReading => _phase == Phase.Reading;
    public bool HasError => _phase == Phase.Failed;
    public bool IsSheetVisible => IsReading || HasError;
    public bool HasReadingLine => IsReading && Source is { ReadingLine: var line } && line.HasValue();
    public bool HasHeard => Heard.HasValue();

    /// <summary>Idle to the camera segment's menu.</summary>
    public DraftCapture ChoosePhoto() => IsIdle ? new(Phase.Choosing, DraftSource.Photo, "", "") : this;

    /// <summary>Menu to the camera or gallery in front. A second tap on a menu item finds no menu and does nothing.</summary>
    public DraftCapture PickPhoto() => IsChoosingPhoto ? new(Phase.Picking, DraftSource.Photo, "", "") : this;

    /// <summary>Idle to recording.</summary>
    public DraftCapture Listen() => IsIdle ? new(Phase.Listening, DraftSource.Voice, "", "") : this;

    /// <summary>Idle, listening or picking to reading. Idle covers a voice start that failed: its error still needs
    /// the sheet. The source of a recording or of a picked photo cannot change on the way.</summary>
    public DraftCapture Read(DraftSource source)
    {
        var canRead = IsIdle || (_phase is Phase.Listening or Phase.Picking && Source == source);
        return canRead ? new(Phase.Reading, source, "", "") : this;
    }

    /// <summary>Reading to the error, which stays until Chiudi.</summary>
    public DraftCapture Fail(Error error, string heard = "") =>
        IsReading ? new(Phase.Failed, Source, error.Message, heard) : this;

    /// <summary>Reading to idle: the editor takes over.</summary>
    public DraftCapture Finish() => IsReading ? Idle : this;

    /// <summary>Anything but reading to idle. A reading cannot be cancelled, so its sheet stays until it ends.</summary>
    public DraftCapture Close() => IsReading ? this : Idle;

    private enum Phase { Idle, Choosing, Picking, Listening, Reading, Failed }
}
