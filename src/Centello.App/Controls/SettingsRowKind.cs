namespace Centello.App.Controls;

/// <summary>What touching a settings line does: nothing, run an action here, open another screen, or ask to flip a switch.</summary>
public enum SettingsRowKind
{
    Info,
    Action,
    Navigation,
    Toggle
}
