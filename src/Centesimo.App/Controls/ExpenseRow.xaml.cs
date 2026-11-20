using Centesimo.Application;

namespace Centesimo.App.Controls;

/// <summary>One expense in a list: the tile of its category, what it is, the detail the list adds and the amount.
/// A tap opens it. The row binds to its item by name (ExpenseId, Title, Detail, Amount, IconKey, ColorKey,
/// Description), so the home and the histories draw the same row from their own items.</summary>
public partial class ExpenseRow : ContentView
{
    public static readonly BindableProperty ExpenseIdProperty = BindableProperty.Create(nameof(ExpenseId), typeof(Guid),
        typeof(ExpenseRow), Guid.Empty);

    public ExpenseRow()
    {
        InitializeComponent();
        this.SetBinding(ExpenseIdProperty, nameof(HistoryRow.ExpenseId));
    }

    /// <summary>The row was tapped: the expense to open.</summary>
    public event EventHandler<Guid>? Opened;

    public Guid ExpenseId { get => (Guid)GetValue(ExpenseIdProperty); set => SetValue(ExpenseIdProperty, value); }

    private void OnClicked(object? sender, EventArgs e) => Opened?.Invoke(this, ExpenseId);
}
