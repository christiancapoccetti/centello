using Centesimo.Domain;

namespace Centesimo.App.Controls;

/// <summary>A one-line text field with its label on the outline and its error underneath.</summary>
public partial class TextField : ContentView
{
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string), typeof(TextField), "");
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(TextField), "", BindingMode.TwoWay);
    public static readonly BindableProperty PlaceholderProperty = BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(TextField), "");
    public static readonly BindableProperty MaxLengthProperty = BindableProperty.Create(nameof(MaxLength), typeof(int), typeof(TextField), int.MaxValue);
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
    public string ErrorText { get => (string)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    private void OnFocusChanged(object? sender, FocusEventArgs e) => ApplyOutline();

    private void OnBoxTapped(object? sender, TappedEventArgs e)
    {
        if (!IsReadOnly)
            Input.Focus();
    }

    private void ApplyOutline() =>
        FieldOutline.Apply(Box, Caption, Input.IsFocused && !IsReadOnly, ErrorText.HasValue(), IsReadOnly);
}
