namespace Centesimo.App.Controls;

/// <summary>The end of a list that scrolls under the transparent navigation bar: a gap, plus the room of the bar, so
/// the last row can scroll clear of it. The page leaves its bottom edge alone (<c>EdgeToEdgePage</c>) and this
/// footer is where the inset is taken.</summary>
public sealed class ListEnd : ContentView
{
    public static readonly BindableProperty GapProperty = BindableProperty.Create(nameof(Gap), typeof(double),
        typeof(ListEnd), 24d, propertyChanged: (end, _, gap) => ((ListEnd)end).Apply((double)gap));

    private readonly BoxView _gap = new() { Color = Colors.Transparent, HeightRequest = 24 };

    public ListEnd()
    {
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.None, SafeAreaRegions.None, SafeAreaRegions.None,
            SafeAreaRegions.Container);
        Content = _gap;
    }

    public double Gap { get => (double)GetValue(GapProperty); set => SetValue(GapProperty, value); }

    private void Apply(double gap) => _gap.HeightRequest = gap;
}
