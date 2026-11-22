using Centello.Domain;

namespace Centello.Application;

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
    public string Heard { get; }

    public bool IsIdle => _phase == Phase.Idle;
    public bool IsChoosingPhoto => _phase == Phase.Choosing;
    public bool IsListening => _phase == Phase.Listening;
    public bool IsReading => _phase == Phase.Reading;
    public bool HasError => _phase == Phase.Failed;
    public bool IsSheetVisible => IsReading || HasError;
    public bool HasReadingLine => IsReading && Source is { ReadingLine: var line } && line.HasValue();
    public bool HasHeard => Heard.HasValue();

    public DraftCapture ChoosePhoto() => IsIdle ? new(Phase.Choosing, DraftSource.Photo, "", "") : this;

    public DraftCapture Pick(DraftSource source) => IsChoosingPhoto ? new(Phase.Picking, source, "", "") : this;

    public DraftCapture Listen() => IsIdle ? new(Phase.Listening, DraftSource.Voice, "", "") : this;

    public DraftCapture Read(DraftSource source)
    {
        var canRead = IsIdle || (_phase is Phase.Listening or Phase.Picking && Source == source);
        return canRead ? new(Phase.Reading, source, "", "") : this;
    }

    public DraftCapture Fail(Error error, string heard = "") =>
        IsReading ? new(Phase.Failed, Source, error.Message, heard) : this;

    public DraftCapture Finish() => IsReading ? Idle : this;

    public DraftCapture Close() => IsReading ? this : Idle;

    private enum Phase { Idle, Choosing, Picking, Listening, Reading, Failed }
}
