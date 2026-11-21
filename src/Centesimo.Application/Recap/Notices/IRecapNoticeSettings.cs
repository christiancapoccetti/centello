namespace Centesimo.Application;

/// <summary>The recap notification's stored state. Written only by <see cref="RecapNoticeAutomation"/>.</summary>
public interface IRecapNoticeSettings
{
    RecapNoticeState State { get; set; }

    /// <summary>The key of the last notice posted, or skipped for lack of data: the dedupe.</summary>
    string LastHandledKey { get; set; }
}
