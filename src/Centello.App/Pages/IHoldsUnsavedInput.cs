namespace Centello.App.Pages;

/// <summary>A page that would lose what was typed if it were closed. AppEntryRouter asks before closing one.</summary>
public interface IHoldsUnsavedInput
{
    bool HasUnsavedInput { get; }
}
