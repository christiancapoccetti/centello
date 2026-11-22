using Centello.App.Theming;
using Centello.Application;
using Centello.Domain;

namespace Centello.App.Controls;

/// <summary>The amount an editor opens on: large condensed digits with the euro sign beside them, on one rule that
/// turns to the alert color with the error underneath.</summary>
public partial class AmountField : ContentView
{
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(AmountField), "", BindingMode.TwoWay);
    public static readonly BindableProperty ErrorTextProperty = BindableProperty.Create(nameof(ErrorText), typeof(string),
        typeof(AmountField), "", propertyChanged: (field, _, _) => ((AmountField)field).ApplyRule());
    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool),
        typeof(AmountField), false, propertyChanged: (field, _, _) => ((AmountField)field).ApplyRule());

    public static readonly BindableProperty IsCompactProperty = BindableProperty.Create(nameof(IsCompact), typeof(bool), typeof(AmountField), false);

    public AmountField()
    {
        InitializeComponent();
        // An entry keeps the width it was measured at, and what is typed after the first figures would scroll out of it.
        Input.TextChanged += (_, _) => ((IView)Input).InvalidateMeasure();
        ApplyRule();
    }

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string ErrorText { get => (string)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public bool IsCompact { get => (bool)GetValue(IsCompactProperty); set => SetValue(IsCompactProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>Puts the cursor in the amount and opens the keypad, as soon as the entry is on screen.</summary>
    public void FocusInput()
    {
        if (Input.IsLoaded)
            Input.Focus();
        else
            Input.Loaded += OnInputLoaded;
    }

    private void OnInputLoaded(object? sender, EventArgs e)
    {
        Input.Loaded -= OnInputLoaded;
        Input.Focus();
    }

    private void OnRowTapped(object? sender, TappedEventArgs e)
    {
        if (!IsReadOnly)
            FocusInput();
    }

    private void ApplyRule()
    {
        var color = ErrorText.HasValue() ? ThemeColor.Alert : IsReadOnly ? ThemeColor.Hairline : ThemeColor.Ink;
        Underline.SetThemeColor(BoxView.ColorProperty, color);
    }
}
