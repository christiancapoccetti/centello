using Centello.App.Theming;
using Centello.Application;

namespace Centello.App.Controls;

internal sealed class RootTabLayout : Grid
{
    private const double RailWidth = 92;
    private readonly ContentPage _page;
    private readonly Grid _rail;
    private readonly ColumnDefinition _railColumn = new(0);
    private bool _isLandscape;

    public RootTabLayout(ContentPage page, IReadOnlyList<ShellContent> destinations, ShellContent selected, Action<int> select)
    {
        _page = page;
        ColumnDefinitions = [_railColumn, new(GridLength.Star)];

        var content = page.Content ?? throw new InvalidOperationException("Root tab content is required.");
        page.Content = null;
        this.Add(content, 1, 0);

        var buttons = new VerticalStackLayout { Spacing = 2, Padding = new Thickness(4, 2, 4, 2), VerticalOptions = LayoutOptions.Center };
        for (var index = 0; index < destinations.Count; index++)
        {
            var destination = destinations[index];
            var button = Destination(destination, destination == selected);
            var target = index;
            button.Clicked += (_, _) => select(target);
            buttons.Add(button);
        }

        var border = new BoxView { WidthRequest = 1, HorizontalOptions = LayoutOptions.End, InputTransparent = true };
        border.SetThemeColor(BoxView.ColorProperty, ThemeColor.Hairline);
        var scroller = new ScrollView
        {
            Content = buttons,
            Orientation = ScrollOrientation.Vertical,
            VerticalScrollBarVisibility = ScrollBarVisibility.Never
        };
        _rail = new Grid { IsVisible = false, Children = { scroller, border } };
        _rail.SetThemeColor(BackgroundColorProperty, ThemeColor.Ground);
        this.Add(_rail, 0, 0);

        page.Loaded += Update;
        page.SizeChanged += Update;
    }

    private static Button Destination(ShellContent destination, bool selected)
    {
        var glyph = ((FontImageSource)destination.Icon).Glyph;
        var ink = selected ? ThemeColor.Ink : ThemeColor.Mute;
        var icon = new FontImageSource { Glyph = glyph, FontFamily = Fonts.Icons, Size = 24 };
        icon.SetThemeColor(FontImageSource.ColorProperty, ink);

        var button = new Button
        {
            ImageSource = icon,
            Text = destination.Title,
            ContentLayout = new Button.ButtonContentLayout(Button.ButtonContentLayout.ImagePosition.Top, 2),
            FontFamily = Fonts.SemiBold,
            FontSize = 11,
            CornerRadius = 16,
            Padding = new Thickness(0, 4),
            MinimumHeightRequest = 52,
            MinimumWidthRequest = 48,
            BackgroundColor = Colors.Transparent
        };
        button.SetThemeColor(Button.TextColorProperty, ink);
        if (selected)
        {
            button.SetThemeColor(Button.BackgroundColorProperty, ThemeColor.SurfaceRaised);
            SemanticProperties.SetDescription(button, $"{destination.Title}, selezionato");
        }

        return button;
    }

    private void Update(object? sender, EventArgs e)
    {
        var landscape = PhoneLandscape.IsActive(_page);
        if (_isLandscape == landscape)
            return;

        _isLandscape = landscape;
        _rail.IsVisible = landscape;
        _railColumn.Width = new GridLength(landscape ? RailWidth : 0);
        Shell.SetTabBarIsVisible(_page, !landscape);
    }
}
