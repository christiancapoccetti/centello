using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App.Controls;

/// <summary>A date written in full in an outlined field, with its label on the outline and its error underneath.
/// A tap opens the native date picker.</summary>
public partial class DateField : ContentView
{
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(nameof(Label), typeof(string),
        typeof(DateField), "", propertyChanged: (field, _, _) => ((DateField)field).Apply());
    public static readonly BindableProperty DateProperty = BindableProperty.Create(nameof(Date), typeof(DateTime),
        typeof(DateField), DateTime.Today, BindingMode.TwoWay, propertyChanged: (field, _, _) => ((DateField)field).Apply());
    public static readonly BindableProperty ErrorTextProperty = BindableProperty.Create(nameof(ErrorText), typeof(string),
        typeof(DateField), "", propertyChanged: (field, _, _) => ((DateField)field).Apply());
    public static readonly BindableProperty IsReadOnlyProperty = BindableProperty.Create(nameof(IsReadOnly), typeof(bool),
        typeof(DateField), false, propertyChanged: (field, _, _) => ((DateField)field).Apply());

    public DateField()
    {
        InitializeComponent();
        Caption.SizeChanged += (_, _) => FieldOutline.PlaceBox(Box, Caption);
        Apply();
    }

    public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    public DateTime Date { get => (DateTime)GetValue(DateProperty); set => SetValue(DateProperty, value); }
    public string ErrorText { get => (string)GetValue(ErrorTextProperty); set => SetValue(ErrorTextProperty, value); }
    public bool IsReadOnly { get => (bool)GetValue(IsReadOnlyProperty); set => SetValue(IsReadOnlyProperty, value); }

    private void Apply()
    {
        Value.Text = ItalianFormat.FullDate(DateOnly.FromDateTime(Date));
        Picker.Date = Date.Date;
        SemanticProperties.SetDescription(Box, $"{Label}: {Value.Text}");
        FieldOutline.Apply(Box, Caption, isFocused: false, ErrorText.HasValue(), IsReadOnly);
    }

    private void OnBoxTapped(object? sender, TappedEventArgs e)
    {
        if (!IsReadOnly)
            Picker.IsOpen = true;
    }

    private void OnDateSelected(object? sender, DateChangedEventArgs e)
    {
        if (e.NewDate is { } picked)
            Date = picked.Date;
    }
}
