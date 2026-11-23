using Centello.App.Theming;
using Centello.Application;

namespace Centello.App.Controls.Recap;

internal sealed class SpendingMapView : Grid
{
    private readonly View[] _tapSurfaces;

    public SpendingMapView(RecapCard.SpendingMap map, bool landscape, StoryInk ink, Action share)
    {
        BackgroundColor = ink.Ground;
        SafeAreaEdges = new SafeAreaEdges(SafeAreaRegions.Container);
        Padding = new Thickness(24, RecapCardViews.TopBar + 8, 24, landscape ? 6 : 18);
        RowDefinitions = [new(GridLength.Star), new(GridLength.Auto)];
        RowSpacing = 8;
        var art = new SpendingMapArt(map, landscape, false, ink.IsDark);
        var shareButton = StoryBlocks.Pill("Condividi", true, ink, share);
        if (landscape)
        {
            _tapSurfaces = [ShowComplete(art, ink, shareButton)];
            return;
        }

        var chart = new GraphicsView { Drawable = new Miniature(art), HeightRequest = 180 };
        SemanticProperties.SetDescription(chart, art.Describe());
        var chartLabels = new Grid
        {
            ColumnDefinitions = [new(GridLength.Star), new(GridLength.Star), new(GridLength.Star)]
        };
        var categoryLabel = StoryBlocks.Caption("Categoria", ink, 13);
        categoryLabel.HorizontalTextAlignment = TextAlignment.Center;
        var tagLabel = StoryBlocks.Caption("Tag", ink, 13);
        tagLabel.HorizontalTextAlignment = TextAlignment.End;
        chartLabels.Add(StoryBlocks.Caption("Tipo", ink, 13));
        chartLabels.Add(categoryLabel, 1);
        chartLabels.Add(tagLabel, 2);
        var stack = new VerticalStackLayout
        {
            Spacing = 12,
            Children =
            {
                StoryBlocks.Headline(art.FocusHeadline, ink),
                StoryBlocks.Caption(map.PeriodLabel, ink, 14),
                chartLabels,
                chart,
                StoryBlocks.Lead(art.FocusCaption, ink),
                StoryBlocks.Caption("Ruota il telefono per leggere la mappa completa.", ink, 14)
            }
        };
        _tapSurfaces = [stack, chart];
        this.Add(new ScrollView { Content = stack, VerticalOptions = LayoutOptions.Center });
        this.Add(shareButton, 0, 1);
    }

    public void EnableTapNavigation(EventHandler<TappedEventArgs> onTapped)
    {
        // Android scroll views handle touches before they reach the story host.
        foreach (var surface in _tapSurfaces)
        {
            var tap = new TapGestureRecognizer();
            tap.Tapped += onTapped;
            surface.GestureRecognizers.Add(tap);
        }
    }

    private GraphicsView ShowComplete(SpendingMapArt art, StoryInk ink, Button shareButton)
    {
        var chart = new GraphicsView { Drawable = art };
        SemanticProperties.SetDescription(chart, art.Describe());
        var viewer = new ScrollView { Content = chart };
        var zoomed = false;
        var zoom = StoryBlocks.Pill("Ingrandisci", false, ink);
        zoom.Clicked += (_, _) =>
        {
            zoomed = !zoomed;
            zoom.Text = zoomed ? "Adatta alla larghezza" : "Ingrandisci";
            viewer.Orientation = zoomed ? ScrollOrientation.Both : ScrollOrientation.Vertical;
            Resize();
        };
        var buttons = new HorizontalStackLayout
        {
            Spacing = 12,
            HorizontalOptions = LayoutOptions.End,
            Children = { zoom, shareButton }
        };
        viewer.SizeChanged += (_, _) => Resize();
        this.Add(viewer);
        this.Add(buttons, 0, 1);
        return chart;

        void Resize()
        {
            if (viewer.Width <= 0)
                return;

            // Keep the labels readable when the story controls leave a short landscape viewport.
            var fit = viewer.Width / art.DesignSize.Width;
            var scale = zoomed ? Math.Max(1, fit * 2) : fit;
            chart.WidthRequest = art.DesignSize.Width * scale;
            chart.HeightRequest = art.DesignSize.Height * scale;
        }
    }

    private sealed class Miniature(SpendingMapArt art) : IDrawable
    {
        public void Draw(ICanvas canvas, RectF dirtyRect) => art.DrawMiniature(canvas, dirtyRect);
    }
}
