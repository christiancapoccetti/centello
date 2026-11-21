namespace Centesimo.App;

/// <summary>Whether a screen reader is on, so what moves by itself can wait for the person.</summary>
public interface IScreenReader
{
    bool IsOn { get; }
}

/// <summary>For platforms without a screen reader to ask.</summary>
public sealed class NoScreenReader : IScreenReader
{
    public bool IsOn => false;
}
