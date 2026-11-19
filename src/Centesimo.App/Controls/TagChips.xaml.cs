using System.Collections;

namespace Centesimo.App.Controls;

/// <summary>The tags of the chosen category as one row of chips. The row keeps its height without tags and says why
/// it is empty, so choosing a category never moves what is below. Items expose Name and IsSelected.</summary>
public partial class TagChips : ContentView
{
    public static readonly BindableProperty ItemsProperty = BindableProperty.Create(nameof(Items), typeof(IEnumerable), typeof(TagChips));
    public static readonly BindableProperty HintProperty = BindableProperty.Create(nameof(Hint), typeof(string), typeof(TagChips), "");

    public TagChips() => InitializeComponent();

    public event EventHandler<object>? TagTapped;

    public IEnumerable? Items { get => (IEnumerable?)GetValue(ItemsProperty); set => SetValue(ItemsProperty, value); }
    public string Hint { get => (string)GetValue(HintProperty); set => SetValue(HintProperty, value); }

    private void OnChipTapped(object? sender, EventArgs e)
    {
        if (IsEnabled && sender is BindableObject { BindingContext: { } tag })
            TagTapped?.Invoke(this, tag);
    }
}
