using Android.Views;
using Android.Widget;
using Centello.App.Theming;
using Centello.Application;
using Centello.Domain;
using AndroidBitmap = Android.Graphics.Bitmap;

namespace Centello.App;

/// <summary>The alert sentence on one line and in full, or neither when nothing is over budget.</summary>
internal readonly record struct AlertArt(AndroidBitmap? OneLine, AndroidBitmap? Full);

internal static class WidgetViews
{
    private const float ChipGlyphDp = 16;
    private const float ChipTextDp = 12;
    private const float OverGlyphDp = 14;
    private const float OverTextDp = 12;
    private const float OverGapDp = 4;

    internal static WidgetTextStyle ChipText => new(Fonts.SemiBold, ChipTextDp);

    /// <summary>The comparison chip of layout/widget_comparison.xml. Lower takes the positive container with its own
    /// text colour and south_east; Higher and Same the raised chip with north_east or east. No comparison hides it.</summary>
    public static void Comparison(RemoteViews views, WidgetArt art, PeriodComparison? comparison, string? text)
    {
        if (comparison is null || text is null)
        {
            views.SetViewVisibility(Resource.Id.comparison, ViewStates.Gone);
            return;
        }

        var lower = comparison.Trend == ComparisonTrend.Lower;
        views.SetViewVisibility(Resource.Id.comparison, ViewStates.Visible);
        views.SetViewVisibility(Resource.Id.comparison_raised, lower ? ViewStates.Gone : ViewStates.Visible);
        views.SetViewVisibility(Resource.Id.comparison_positive, lower ? ViewStates.Visible : ViewStates.Gone);
        views.SetImageViewBitmap(lower ? Resource.Id.comparison_glyph_low : Resource.Id.comparison_glyph,
            art.Glyph(Arrow(comparison.Trend), ChipGlyphDp));
        views.SetImageViewBitmap(lower ? Resource.Id.comparison_text_low : Resource.Id.comparison_text,
            art.Text(text, ChipText));
    }

    /// <summary>The alert sentence of a category over budget, drawn both on one line cut with an ellipsis and in full,
    /// wrapped inside <paramref name="widthDp"/> less its glyph: the card picks one by the room it has. Both are null
    /// when nothing is over budget.</summary>
    public static AlertArt Alert(WidgetArt art, string sentence, float widthDp)
    {
        if (sentence.IsEmpty())
            return new AlertArt(null, null);

        var style = new WidgetTextStyle(Fonts.SemiBold, OverTextDp);
        var textWidth = widthDp - OverGlyphDp - OverGapDp;
        var full = art.Paragraph(sentence, style, textWidth);
        // A word wider than the card stays whole on its line, so wrapping cannot make the sentence fit.
        return new AlertArt(art.Text(sentence, style, textWidth), WidgetArt.WidthDp(full) <= textWidth ? full : null);
    }

    /// <summary>The Error glyph and the sentence in alert; hidden when there is no sentence to draw, because nothing
    /// is over budget or the card has no room for it.</summary>
    public static void OverBudget(RemoteViews views, WidgetArt art, AndroidBitmap? sentence)
    {
        if (sentence is null)
        {
            views.SetViewVisibility(Resource.Id.over_row, ViewStates.Gone);
            return;
        }

        views.SetViewVisibility(Resource.Id.over_row, ViewStates.Visible);
        views.SetImageViewBitmap(Resource.Id.over_glyph, art.Glyph(MaterialSymbols.Error, OverGlyphDp));
        views.SetImageViewBitmap(Resource.Id.over_text, sentence);
    }

    private static string Arrow(ComparisonTrend trend) => trend switch
    {
        ComparisonTrend.Lower => MaterialSymbols.SouthEast,
        ComparisonTrend.Higher => MaterialSymbols.NorthEast,
        _ => MaterialSymbols.East
    };
}
