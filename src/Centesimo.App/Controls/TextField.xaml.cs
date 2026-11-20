using Centesimo.Domain;

namespace Centesimo.App.Controls;

/// <summary>A one-line text field with its label on the outline. Under it goes its error or, while there is none,
/// the line that helps to fill it. A suffix names the unit of what is typed.</summary>
public partial class TextField : ContentView
{
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(TextField), "");
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(TextField), "", BindingMode.TwoWay);
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(TextField), "");
    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(TextField), int.MaxValue);
    public static readonly BindableProperty SuffixProperty = BindableProperty.Create(nameof(Suffix), typeof(string), typeof(TextField), "");
    public static readonly BindableProperty KeyboardProperty = BindableProperty.Create(nameof(Keyboard), typeof(Keyboard), typeof(TextField), Keyboard.Default);
    public static readonly BindableProperty HelperTextProperty = BindableProperty.Create(nameof(HelperText), typeof(string),
        typeof(TextField), "", propertyChanged: (field, _, _) => ((TextField)field).ApplyOutline());
    public static readonly BindableProperty ErrorTextProperty = BindableProperty.Create(nameof(ErrorText), typeof(string),
        typeof(TextField), "", propertyChanged: (field, _, _) => ((TextField)field).ApplyOutline());
    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool),
        typeof(TextField), false, propertyChanged: (field, _, _) => ((TextField)field).ApplyOutline());

    public TextField()
    {
        InitializeComponent();
        ApplyOutline();
    }

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Placeholder { get => (string)GetValue(PlaceholderProperty); set => SetValue(PlaceholderProperty, value); }
    public int MaxLength { get => (int)GetValue(MaxLengthProperty); set => SetValue(MaxLengthProperty, value); }
    public string Suffix { get => (string)GetValue(SuffixProperty); set => SetValue(SuffixProperty, value); }
    [System.ComponentModel.TypeConverter(typeof(Microsoft.Maui.Converters.KeyboardTypeConverter))]
    public Keyboard Keyboard { get => (Keyboard)GetValue(KeyboardProperty); set => SetValue(KeyboardProperty, value); }
    public string HelperText { get => (string)GetValue(HelperTextProperty); set => SetValue(HelperTextProperty, value); }
    public string ErrorText { get => (string)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    /// <summary>The keyboard confirmed what was typed.</summary>
    public event EventHandler? Completed;

    /// <summary>Puts the cursor in the field and opens the keyboard.</summary>
    public void FocusInput() => Input.Focus();

    private void OnFocusChanged(object? sender, FocusEventArgs e) => ApplyOutline();

    private void OnCompleted(object? sender, EventArgs e) => Completed?.Invoke(this, EventArgs.Empty);

    private void OnBoxTapped(object? sender, TappedEventArgs e)
    {
        if (!IsReadOnly)
            Input.Focus();
    }

    private void ApplyOutline()
    {
        FieldOutline.Apply(Box, Caption, Input.IsFocused && !IsReadOnly, ErrorText.HasValue(), IsReadOnly);
        // The help is for filling the field in: it leaves when the error has the line, or nothing can be typed.
        Helper.IsVisible = HelperText.HasValue() && ErrorText.IsEmpty() && !IsReadOnly;
    }
}
