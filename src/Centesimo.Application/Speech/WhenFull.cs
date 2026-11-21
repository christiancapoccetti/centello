namespace Centesimo.Application;

/// <summary>What a dictation does with a recording that reached the limit. Held on Oggi, the limit cut the phrase,
/// so it is refused. The voice panel ends at the limit on purpose, so what it heard is sent.</summary>
public enum WhenFull { Refuse, Send }
