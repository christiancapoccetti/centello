using Centello.Application;
using Microsoft.Maui.Layouts;

namespace Centello.App.Controls;

/// <summary>A block and the value that closes it, an amount: two children, the block first. The value takes the
/// width it needs at the end of the row and the block the rest, both centred on the row. When the block would keep
/// less than <see cref="MinContentWidth"/>, as it does with a large system font, the value moves to the end of a line
/// of its own under the block, so neither is squeezed to a word a line or cut.</summary>
public sealed class TrailingValueLayout : Layout
{
    public static readonly BindableProperty SpacingProperty = BindableProperty.Create(nameof(Spacing), typeof(double),
        typeof(TrailingValueLayout), 14d, propertyChanged: (layout, _, _) => ((TrailingValueLayout)layout).InvalidateMeasure());
    public static readonly BindableProperty MinContentWidthProperty = BindableProperty.Create(nameof(MinContentWidth),
        typeof(double), typeof(TrailingValueLayout), 120d, propertyChanged: (layout, _, _) => ((TrailingValueLayout)layout).InvalidateMeasure());

    /// <summary>The gap between the block and the value, beside or under.</summary>
    public double Spacing { get => (double)GetValue(SpacingProperty); set => SetValue(SpacingProperty, value); }

    /// <summary>The narrowest the block may get beside the value.</summary>
    public double MinContentWidth { get => (double)GetValue(MinContentWidthProperty); set => SetValue(MinContentWidthProperty, value); }

    protected override ILayoutManager CreateLayoutManager() => new TrailingValueLayoutManager(this);

    private sealed class TrailingValueLayoutManager(TrailingValueLayout layout) : LayoutManager(layout)
    {
        private readonly record struct Plan(ValuePlacement Placement, Size Block, Size Value);

        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            if (layout.Count < 2)
                return Size.Zero;

            var plan = PlanFor(widthConstraint);
            return plan.Placement == ValuePlacement.Beside
                ? new Size(plan.Block.Width + layout.Spacing + plan.Value.Width, Math.Max(plan.Block.Height, plan.Value.Height))
                : new Size(Math.Max(plan.Block.Width, plan.Value.Width), plan.Block.Height + layout.Spacing / 2 + plan.Value.Height);
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            if (layout.Count < 2)
                return bounds.Size;

            var plan = PlanFor(bounds.Width);
            if (plan.Placement == ValuePlacement.Beside)
            {
                layout[0].Arrange(new Rect(bounds.X, bounds.Y + (bounds.Height - plan.Block.Height) / 2,
                    bounds.Width - plan.Value.Width - layout.Spacing, plan.Block.Height));
                layout[1].Arrange(new Rect(bounds.Right - plan.Value.Width, bounds.Y + (bounds.Height - plan.Value.Height) / 2,
                    plan.Value.Width, plan.Value.Height));
                return bounds.Size;
            }

            var gap = layout.Spacing / 2;
            var top = bounds.Y + (bounds.Height - plan.Block.Height - gap - plan.Value.Height) / 2;
            layout[0].Arrange(new Rect(bounds.X, top, bounds.Width, plan.Block.Height));
            layout[1].Arrange(new Rect(bounds.Right - plan.Value.Width, top + plan.Block.Height + gap, plan.Value.Width,
                plan.Value.Height));
            return bounds.Size;
        }

        private Plan PlanFor(double width)
        {
            var value = layout[1].Measure(width, double.PositiveInfinity);
            var placement = TrailingValuePlacement.Of(width, value.Width, layout.Spacing, layout.MinContentWidth);
            var blockWidth = placement == ValuePlacement.Beside ? width - value.Width - layout.Spacing : width;
            return new Plan(placement, layout[0].Measure(blockWidth, double.PositiveInfinity), value);
        }
    }
}
