using Centello.Application;

namespace Centello.App.Controls;

/// <summary>A date as three chips: today, yesterday or a date from the native picker. Most expenses are from today or
/// yesterday, so the picker opens only when asked.</summary>
public partial class DateChips : ContentView
{
    public static readonly BindableProperty DateProperty = BindableProperty.Create(nameof(Date), typeof(DateTime),
        typeof(DateChips), DateTime.Today, BindingMode.TwoWay, propertyChanged: (chips, _, _) => ((DateChips)chips).Apply());
    public static readonly BindableProperty MaximumDateProperty = BindableProperty.Create(nameof(MaximumDate), typeof(DateTime?),
        typeof(DateChips), propertyChanged: (chips, _, _) => ((DateChips)chips).Apply());

    public DateChips()
    {
        InitializeComponent();
        Apply();
    }

    public DateTime Date { get => (DateTime)GetValue(DateProperty); set => SetValue(DateProperty, value); }
    public DateTime? MaximumDate { get => (DateTime?)GetValue(MaximumDateProperty); set => SetValue(MaximumDateProperty, value); }

    private void Apply()
    {
        var date = Date.Date;
        var isToday = date == DateTime.Today;
        var isYesterday = date == DateTime.Today.AddDays(-1);
        TodayChip.IsSelected = isToday;
        YesterdayChip.IsSelected = isYesterday;
        OtherChip.IsSelected = !isToday && !isYesterday;
        OtherChip.Text = OtherChip.IsSelected ? ItalianFormat.WeekdayDate(DateOnly.FromDateTime(date)) : "Altra data";
        Picker.MaximumDate = MaximumDate;
        Picker.Date = date;
    }

    private void OnTodayTapped(object? sender, EventArgs e) => Date = DateTime.Today;

    private void OnYesterdayTapped(object? sender, EventArgs e) => Date = DateTime.Today.AddDays(-1);

    private void OnOtherTapped(object? sender, EventArgs e) => Picker.IsOpen = true;

    private void OnDateSelected(object? sender, DateChangedEventArgs e)
    {
        if (e.NewDate is { } picked)
            Date = picked.Date;
    }
}
