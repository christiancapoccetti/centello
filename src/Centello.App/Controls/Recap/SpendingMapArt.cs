using Centello.App.Theming;
using Centello.Application;
using HAlign = Microsoft.Maui.Graphics.HorizontalAlignment;
using VAlign = Microsoft.Maui.Graphics.VerticalAlignment;
using GraphicsFont = Microsoft.Maui.Graphics.Font;

namespace Centello.App.Controls.Recap;

internal sealed partial class SpendingMapArt : IDrawable
{
    private const float PanelWidth = 466;
    private const float Margin = 24;
    private const float Header = 98;
    private readonly RecapCard.SpendingMap _map;
    private readonly StoryInk _ink;
    private readonly bool _complete;
    private readonly bool _amounts;
    private readonly List<List<SpendingMapSlice>> _panels;
    private readonly int _columns;
    private readonly float _panelHeight;

    public SpendingMapArt(RecapCard.SpendingMap map, bool complete, bool amounts, bool isDark)
    {
        _map = map;
        _ink = StoryInk.Of(RecapSurface.Ground, isDark);
        _complete = complete;
        _amounts = amounts;
        _panels = complete ? SpendingMapPanels.Pack(map.Categories) : [];
        if (!complete)
        {
            DesignSize = new Size(400, 500);
            return;
        }

        _panelHeight = 50 + _panels.Max(panel => SpendingMapPanels.Layout(panel, map.TotalCents).Height);
        _columns = Math.Clamp((int)Math.Ceiling(Math.Sqrt(_panels.Count * 1.65 * _panelHeight / PanelWidth)), 1, _panels.Count);
        var rows = (int)Math.Ceiling((double)_panels.Count / _columns);
        var height = Header + rows * _panelHeight + Margin;
        DesignSize = new Size(Math.Max(_columns * PanelWidth + 2 * Margin, height * 1.5), height);
    }

    public Size DesignSize { get; }

    public string Value(long cents) => _amounts ? ItalianFormat.Money(cents) : _map.Share(cents);

    public void Draw(ICanvas canvas, RectF bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        canvas.SaveState();
        canvas.FillColor = _ink.Ground;
        canvas.FillRectangle(bounds);
        var scale = (float)Math.Min(bounds.Width / DesignSize.Width, bounds.Height / DesignSize.Height);
        canvas.Translate(bounds.Center.X - (float)DesignSize.Width * scale / 2,
            bounds.Center.Y - (float)DesignSize.Height * scale / 2);
        canvas.Scale(scale, scale);
        canvas.Antialias = true;
        var width = (float)DesignSize.Width;
        Text(canvas, "Centello", Margin, 11, 150, 24, 15, _ink.Ink, bold: true);
        Text(canvas, _map.PeriodLabel, width * 0.4f, 11, width * 0.6f - Margin, 24, 14, _ink.Mute, HAlign.Right);
        if (_complete)
            Full(canvas);
        else
            Portrait(canvas);
        canvas.RestoreState();
    }

    private void Portrait(ICanvas canvas)
    {
        var focus = _map.FocusTag;
        var category = Name(_map.FocusCategory.Category.Name, "Categoria");
        var tag = Name(focus.Name, "Tag");
        Text(canvas, $"{tag} · {_map.Share(focus.TotalCents)}", Margin, 48, 352, 84, 32, _ink.Ink, bold: true);
        Text(canvas, _amounts ? $"Spese totali · {Value(_map.TotalCents)}" : "Quota delle spese totali del periodo",
            Margin, 138, 352, 30, 16, _ink.Mute);
        Text(canvas, "Tipo", Margin, 189, 86, 22, 13, _ink.Mute);
        Text(canvas, "Categoria", 147, 189, 106, 22, 13, _ink.Mute, HAlign.Center);
        Text(canvas, "Tag", 290, 189, 86, 22, 13, _ink.Mute, HAlign.Right);
        DrawMiniature(canvas, new RectF(Margin, 220, 352, 180));
        canvas.FillColor = _ink.Palette(_map.FocusCategory.Category.ColorKey);
        canvas.FillRectangle(Margin, 432, 10, 10);
        Text(canvas, $"{category} → {tag} · {Value(focus.TotalCents)}", 43, 411, 333, 65, 16, _ink.Ink, bold: true);
    }

    public void DrawMiniature(ICanvas canvas, RectF bounds)
    {
        if (bounds.Width <= 0 || bounds.Height <= 0)
            return;

        canvas.SaveState();
        canvas.Translate(bounds.Left, bounds.Top);
        canvas.Antialias = true;
        var height = bounds.Height;
        var count = _map.Categories.Sum(category => category.Tags.Count);
        var categoryGap = height * .20f / Math.Max(1, _map.Categories.Count - 1);
        var tagGap = height * .12f / Math.Max(1, count - _map.Categories.Count);
        var mass = height - categoryGap * (_map.Categories.Count - 1) - tagGap * (count - _map.Categories.Count);
        var left = 3f;
        var middle = bounds.Width * .47f;
        var right = bounds.Width - 3;
        var autoTotal = _map.Categories.Sum(category => category.Tags.Sum(tag => tag.AutomaticCents));
        var autoHeight = Thickness(autoTotal, mass);
        var autoY = (height - mass) / 4;
        var variableY = autoY + autoHeight + (height - mass) / 2;
        Node(canvas, 0, autoY, 3, autoHeight, _ink.Mute);
        Node(canvas, 0, variableY, 3, mass - autoHeight, _ink.Mute);
        var top = 0f;
        foreach (var category in _map.Categories)
        {
            var color = _ink.Palette(category.Category.ColorKey);
            var muted = color.WithAlpha(.24f);
            var categoryHeight = Thickness(category.TotalCents, mass);
            var categoryY = top + tagGap * (category.Tags.Count - 1) / 2;
            var variableIn = categoryY + Thickness(category.Tags.Sum(tag => tag.AutomaticCents), mass);
            var autoIn = categoryY;
            var leafY = top;
            var outgoing = categoryY;
            foreach (var tag in category.Tags)
            {
                var isFocus = category.Category.CategoryId == _map.FocusCategory.Category.CategoryId && tag.TagId == _map.FocusTag.TagId;
                var flowColor = isFocus ? color : muted;
                var automatic = Thickness(tag.AutomaticCents, mass);
                var variable = Thickness(tag.VariableCents, mass);
                Ribbon(canvas, left, autoY, middle, autoIn, automatic, flowColor);
                Ribbon(canvas, left, variableY, middle, variableIn, variable, flowColor);
                Ribbon(canvas, middle + 4, outgoing, right, leafY, automatic + variable, flowColor);
                Node(canvas, middle, outgoing, 4, automatic + variable, flowColor);
                Node(canvas, right, leafY, 3, automatic + variable, flowColor);
                autoY += automatic;
                autoIn += automatic;
                variableY += variable;
                variableIn += variable;
                outgoing += automatic + variable;
                leafY += automatic + variable + tagGap;
            }
            top += categoryHeight + tagGap * (category.Tags.Count - 1) + categoryGap;
        }
        canvas.RestoreState();
    }

    private void Full(ICanvas canvas)
    {
        var width = (float)DesignSize.Width;
        Text(canvas, "La mia mappa delle spese", Margin, 36, width - 2 * Margin, 34, 27, _ink.Ink, bold: true);
        Text(canvas, _amounts ? $"Spese totali · {Value(_map.TotalCents)}" : "Percentuali sul totale delle spese del periodo",
            Margin, 72, width - 2 * Margin, 22, 15, _ink.Mute);
        var start = (width - _columns * PanelWidth) / 2;
        for (var i = 0; i < _panels.Count; i++)
        {
            canvas.SaveState();
            canvas.Translate(start + i % _columns * PanelWidth, Header + i / _columns * _panelHeight);
            Panel(canvas, _panels[i]);
            canvas.RestoreState();
        }
    }

    private void Panel(ICanvas canvas, List<SpendingMapSlice> panel)
    {
        const float source = 82;
        const float middle = 144;
        const float bridge = 82;
        const float leaf = 284;
        var layout = SpendingMapPanels.Layout(panel, _map.TotalCents);
        var automatic = panel.Sum(slice => slice.AutomaticCents);
        var variable = panel.Sum(slice => slice.VariableCents);
        var autoHeight = Thickness(automatic, SpendingMapPanels.FullMass);
        var variableHeight = Thickness(variable, SpendingMapPanels.FullMass);
        var autoY = 36 + layout.AutomaticTop;
        var variableY = 36 + layout.VariableTop;

        Text(canvas, "Tipo", 0, 0, 80, 20, 14, _ink.Mute, bold: true);
        Text(canvas, "Categoria", middle - 23, 0, bridge + 46, 20, 14, _ink.Mute, HAlign.Center, true);
        Text(canvas, "Tag", leaf + 9, 0, 120, 20, 14, _ink.Mute, bold: true);
        DrawSource("Automatiche", automatic, autoY, autoHeight);
        DrawSource("Variabili", variable, variableY, variableHeight);
        var row = 36f;
        foreach (var slice in panel)
        {
            var color = _ink.Palette(slice.Category.Category.ColorKey);
            var bandHeight = Thickness(slice.TotalCents, SpendingMapPanels.FullMass);
            var groupHeight = SpendingMapPanels.GroupHeight(slice, _map.TotalCents);
            var center = row + (groupHeight - 10) / 2;
            var band = center - bandHeight / 2;
            var autoThickness = Thickness(slice.AutomaticCents, SpendingMapPanels.FullMass);
            var variableThickness = Thickness(slice.VariableCents, SpendingMapPanels.FullMass);
            Ribbon(canvas, source, autoY, middle, band, autoThickness, color);
            Ribbon(canvas, source, variableY, middle, band + autoThickness, variableThickness, color);
            autoY += autoThickness;
            variableY += variableThickness;
            var next = band;
            var tagRow = row;
            foreach (var tag in slice.Tags)
            {
                var thickness = Thickness(tag.TotalCents, SpendingMapPanels.FullMass);
                var rowHeight = SpendingMapPanels.LeafHeight(tag, _map.TotalCents);
                var leafCenter = tagRow + rowHeight / 2;
                Ribbon(canvas, middle + bridge, next, leaf, leafCenter - thickness / 2, thickness, color);
                Node(canvas, leaf, leafCenter - thickness / 2, 3, thickness, color);
                Text(canvas, Name(tag.Name, "Tag"), leaf + 9, leafCenter - 16, 160, 18, 14, _ink.Ink, bold: true);
                Text(canvas, Value(tag.TotalCents), leaf + 9, leafCenter + 2, 160, 17, 13, _ink.Mute);
                next += thickness;
                tagRow += rowHeight;
            }
            Node(canvas, middle, band, bridge, bandHeight, color);
            var name = Name(slice.Category.Category.Name, "Categoria");
            if (slice.Parts > 1)
                name = $"{slice.Part}/{slice.Parts} · {name}";
            Text(canvas, name, middle - 28, band - 20, bridge + 56, 19, 14, _ink.Ink, HAlign.Center, true);
            var inside = bandHeight >= 19 && !_amounts;
            Text(canvas, Value(slice.TotalCents), middle - 18, inside ? center - 9 : band + bandHeight + 1,
                bridge + 36, 18, 13, inside ? _ink.TileInk : _ink.Mute, HAlign.Center);
            row += groupHeight;
        }

        void DrawSource(string name, long cents, float y, float thickness)
        {
            if (cents == 0)
                return;
            Node(canvas, source - 3, y, 3, thickness, _ink.Mute);
            var center = y + thickness / 2;
            Text(canvas, name, 0, center - 17, source - 6, 18, 13, _ink.Ink, bold: true);
            Text(canvas, Value(cents), 0, center, source - 6, 19, 12, _ink.Mute);
        }
    }

    private float Thickness(long cents, float fullHeight) => SpendingMapPanels.Thickness(cents, _map.TotalCents, fullHeight);

    internal string Name(string text, string fallback) => _amounts ? text : SpendingMapPanels.PrivateLabel(text, fallback);

    private static void Node(ICanvas canvas, float x, float y, float width, float height, Color color)
    {
        canvas.FillColor = color;
        canvas.FillRectangle(x, y, width, height);
    }

    private static void Ribbon(ICanvas canvas, float from, float y, float to, float end, float height, Color color)
    {
        if (height <= 0)
            return;
        var bend = (from + to) / 2;
        var path = new PathF();
        path.MoveTo(from, y);
        path.CurveTo(bend, y, bend, end, to, end);
        path.LineTo(to, end + height);
        path.CurveTo(bend, end + height, bend, y + height, from, y + height);
        path.Close();
        canvas.FillColor = color;
        canvas.FillPath(path);
    }

    private static void Text(ICanvas canvas, string text, float x, float y, float width, float height, float size,
        Color color, HAlign align = HAlign.Left, bool bold = false)
    {
        canvas.Font = bold ? GraphicsFont.DefaultBold : GraphicsFont.Default;
        canvas.FontSize = size;
        canvas.FontColor = color;
        canvas.DrawString(text, x, y, width, height, align, VAlign.Center);
    }
}
