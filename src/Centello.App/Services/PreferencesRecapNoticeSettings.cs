using Centello.Application;

namespace Centello.App;

/// <summary>Where the recap notice keeps its state: "recap-notice-state" holds the name of a
/// <see cref="RecapNoticeState"/> and "recap-notice-last-handled" the key of the last period posted or skipped.
/// Preferences are not in the database, so a restore never brings an old notice back.</summary>
public sealed class PreferencesRecapNoticeSettings : IRecapNoticeSettings
{
    private const string StateKey = "recap-notice-state";
    private const string LastHandledKeyKey = "recap-notice-last-handled";

    public RecapNoticeState State
    {
        get => Enum.TryParse<RecapNoticeState>(Preferences.Default.Get(StateKey, ""), out var state)
            && Enum.IsDefined(state) ? state : RecapNoticeState.Undecided;
        set => Preferences.Default.Set(StateKey, value.ToString());
    }

    public string LastHandledKey
    {
        get => Preferences.Default.Get(LastHandledKeyKey, "");
        set => Preferences.Default.Set(LastHandledKeyKey, value);
    }
}
