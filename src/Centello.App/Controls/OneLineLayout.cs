using Microsoft.Maui.Layouts;

namespace Centello.App.Controls;

/// <summary>A row that never wraps. It arranges its children in order for as long as each one fits whole in the
/// width, and gives the first one that does not, and every one after it, no room. Those leave the accessibility tree,
/// so a screen reader cannot land on an item that is not on screen. An item that is dropped is never clipped.</summary>
public sealed class OneLineLayout : Layout
{
    protected override ILayoutManager CreateLayoutManager() => new OneLineLayoutManager(this);

    private sealed class OneLineLayoutManager(OneLineLayout layout) : LayoutManager(layout)
    {
        public override Size Measure(double widthConstraint, double heightConstraint)
        {
            var sizes = Sizes(heightConstraint);
            var shown = sizes.Take(Fitting(sizes, widthConstraint)).ToList();
            return new Size(shown.Sum(size => size.Width), shown.Count == 0 ? 0 : shown.Max(size => size.Height));
        }

        public override Size ArrangeChildren(Rect bounds)
        {
            var sizes = Sizes(bounds.Height);
            var fitting = Fitting(sizes, bounds.Width);
            var x = bounds.X;
            for (var index = 0; index < sizes.Count; index++)
            {
                var child = Layout[index];
                var shown = index < fitting;
                var size = sizes[index];
                child.Arrange(shown
                    ? new Rect(x, bounds.Y + (bounds.Height - size.Height) / 2, size.Width, size.Height)
                    : new Rect(bounds.X, bounds.Y, 0, 0));
                if (child is BindableObject item)
                    AutomationProperties.SetIsInAccessibleTree(item, shown);

                x += size.Width;
            }

            return bounds.Size;
        }

        private List<Size> Sizes(double heightConstraint) => Layout
            .Select(child => child.Measure(double.PositiveInfinity, heightConstraint))
            .ToList();

        // The leading children that fit: a shorter one further on never takes the place of one that was dropped.
        private static int Fitting(List<Size> sizes, double width)
        {
            var used = 0d;
            var count = 0;
            foreach (var size in sizes)
            {
                used += size.Width;
                if (used > width)
                    break;

                count++;
            }

            return count;
        }
    }
}
