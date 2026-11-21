namespace Centesimo.App;

/// <summary>The fingers going down on and lifting off the screen, told by the platform. A gesture recognizer sees a
/// press only once it is a tap, and the story has to pause the moment the finger lands.</summary>
public sealed class StoryTouches
{
    public event Action? Pressed;
    public event Action? Released;

    public void Press() => Pressed?.Invoke();

    public void Release() => Released?.Invoke();
}
