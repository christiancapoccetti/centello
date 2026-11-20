using Android.Views;
using Android.Widget;
using Centesimo.App.Theming;
using Centesimo.Application;
using Centesimo.Domain;

namespace Centesimo.App;

internal static class WidgetViews
{
    private const float ChipGlyphDp = 16;
    private const float ChipTextDp = 12;
    private const float OverGlyphDp = 14;
    private const float OverTextDp = 12;
    private const float OverGapDp = 4;

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
            art.Text(text, new WidgetTextStyle(Fonts.SemiBold, ChipTextDp)));
    }

    /// <summary>The Error glyph and the sentence in alert, wrapped inside <paramref name="widthDp"/>; hidden when
    /// nothing is over budget.</summary>
    public static void OverBudget(RemoteViews views, WidgetArt art, string sentence, float widthDp)
    {
        if (sentence.IsEmpty())
        {
            views.SetViewVisibility(Resource.Id.over_row, ViewStates.Gone);
            return;
        }

        views.SetViewVisibility(Resource.Id.over_row, ViewStates.Visible);
        views.SetImageViewBitmap(Resource.Id.over_glyph, art.Glyph(MaterialSymbols.Error, OverGlyphDp));
        views.SetImageViewBitmap(Resource.Id.over_text, art.Paragraph(sentence,
            new WidgetTextStyle(Fonts.SemiBold, OverTextDp), widthDp - OverGlyphDp - OverGapDp));
    }

    private static string Arrow(ComparisonTrend trend) => trend switch
    {
        ComparisonTrend.Lower => MaterialSymbols.SouthEast,
        ComparisonTrend.Higher => MaterialSymbols.NorthEast,
        _ => MaterialSymbols.East
    };
}
