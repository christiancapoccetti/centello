using Centesimo.App.Theming;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.Controls;

/// <summary>A one-line text field with its label on the outline. Under it goes its error or, while there is none,
/// the line that helps to fill it. A suffix names the unit of what is typed. A password field hides what is typed
/// and offers an eye to show it.</summary>
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
    public static readonly BindableProperty IsPasswordProperty = BindableProperty.Create(nameof(IsPassword), typeof(bool),
        typeof(TextField), false, propertyChanged: (field, _, _) => ((TextField)field).ApplyReveal());

    private bool _isRevealed;
    private Button? _reveal;

    public TextField()
    {
        InitializeComponent();
        ApplyOutline();
        ApplyReveal();
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
    public bool IsPassword { get => (bool)GetValue(IsPasswordProperty); set => SetValue(IsPasswordProperty, value); }

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

    private void ApplyReveal()
    {
        var isHidden = IsPassword && !_isRevealed;
        Input.IsPassword = isHidden;
        // A key is no word: suggestions would only keep it in the keyboard dictionary.
        Input.IsSpellCheckEnabled = !IsPassword;
        Input.IsTextPredictionEnabled = !IsPassword;
        if (IsPassword)
            ShowReveal(isHidden);
        else if (_reveal is not null)
            _reveal.IsVisible = false;
    }

    // The eye exists only for a password field: every other field would carry a button nobody can see.
    private void ShowReveal(bool isHidden)
    {
        if (_reveal is null)
        {
            _reveal = new Button
            {
                Style = (Style)Microsoft.Maui.Controls.Application.Current!.Resources["IconGlyphButton"],
                WidthRequest = 40,
                HeightRequest = 40,
                // Its 8 of margin on the right take back the padding of the box.
                Margin = new Thickness(8, 0, -8, 0)
            };
            _reveal.SetThemeColor(Button.TextColorProperty, ThemeColor.Mute);
            _reveal.Clicked += (_, _) =>
            {
                _isRevealed = !_isRevealed;
                ApplyReveal();
            };
            Row.Add(_reveal, 1);
        }

        _reveal.IsVisible = true;
        _reveal.Text = isHidden ? MaterialSymbols.Visibility : MaterialSymbols.VisibilityOff;
        SemanticProperties.SetDescription(_reveal, isHidden ? "Mostra il testo" : "Nascondi il testo");
    }
}
