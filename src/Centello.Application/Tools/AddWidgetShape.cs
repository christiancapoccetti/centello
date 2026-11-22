namespace Centello.Application;

/// <summary>The layout the Aggiungi widget takes at a given size.</summary>
public enum AddWidgetShape
{
    /// <summary>The 2x1: plus and microphone.</summary>
    Compact,
    /// <summary>The 3x1: plus, microphone and camera, no words.</summary>
    Icons,
    /// <summary>The 4x1: the add words, microphone and camera.</summary>
    Bar,
    /// <summary>The 4x2: the add pill on top, the voice and photo pills under it.</summary>
    Tall
}

/// <summary>The widths in dp of the words the layouts draw as pictures.</summary>
public readonly record struct AddWidgetWords(float AddDp, float VoiceDp, float PhotoDp);

public static class AddWidgetShapes
{
    // The measures of widget_add.xml, widget_add_compact.xml and widget_add_tall.xml: keep them in step.
    private const float OuterMarginDp = 8;
    private const float IconSegmentDp = 64;
    private const float DividerDp = 1;
    private const float GlyphDp = 26;
    private const float GlyphWordGapDp = 8;
    // The air a word keeps on each side of its pill.
    private const float WordSideAirDp = 16;
    // An icon segment shrinks to this before the camera is dropped.
    private const float MinIconSegmentDp = 56;
    private const float CardPaddingDp = 12;
    private const float RowGapDp = 10;
    private const float MinPillHeightDp = 56;

    /// <summary>The height below which the two pill rows no longer fit the card.</summary>
    public const int TallMinHeightDp = (int)(2 * (OuterMarginDp + CardPaddingDp) + 2 * MinPillHeightDp + RowGapDp);

    private const float WordPillExtraDp = GlyphDp + GlyphWordGapDp + 2 * WordSideAirDp;
    private const float OuterWidthDp = 2 * OuterMarginDp;

    /// <summary>The narrowest widget that shows the add words next to the microphone and the camera.</summary>
    public static float BarMinWidthDp(AddWidgetWords words) =>
        OuterWidthDp + words.AddDp + WordPillExtraDp + 2 * (DividerDp + IconSegmentDp);

    /// <summary>The narrowest widget that shows the plus, microphone and camera segments.</summary>
    public const float IconsMinWidthDp = OuterWidthDp + 3 * MinIconSegmentDp + 2 * DividerDp;

    /// <summary>The narrowest widget whose full-width pill holds the add words and whose half pills hold theirs.</summary>
    public static float TallMinWidthDp(AddWidgetWords words)
    {
        var cardInnerDp = 2 * (OuterMarginDp + CardPaddingDp);
        var halfPills = 2 * (Math.Max(words.VoiceDp, words.PhotoDp) + WordPillExtraDp) + RowGapDp;
        var fullPill = words.AddDp + WordPillExtraDp;
        return cardInnerDp + Math.Max(halfPills, fullPill);
    }

    public static AddWidgetShape Of(int widthDp, int heightDp, AddWidgetWords words)
    {
        if (heightDp >= TallMinHeightDp && widthDp >= TallMinWidthDp(words))
            return AddWidgetShape.Tall;

        if (widthDp >= BarMinWidthDp(words))
            return AddWidgetShape.Bar;

        return widthDp >= IconsMinWidthDp ? AddWidgetShape.Icons : AddWidgetShape.Compact;
    }

    public static bool HasPhoto(this AddWidgetShape shape) => shape != AddWidgetShape.Compact;
}
