using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Centello.Application;

namespace Centello.App.Controls.Recap;

internal sealed record SpendingMapSlice(RecapFlowCategory Category, IReadOnlyList<RecapFlowTag> Tags, int Part, int Parts)
{
    public long AutomaticCents { get; } = Tags.Sum(tag => tag.AutomaticCents);
    public long VariableCents { get; } = Tags.Sum(tag => tag.VariableCents);
    public long TotalCents => AutomaticCents + VariableCents;
}

internal static partial class SpendingMapPanels
{
    public const float FullMass = 115;
    private const int TagsPerPanel = 16;
    private const int CategoriesPerPanel = 4;

    public static List<List<SpendingMapSlice>> Pack(IReadOnlyList<RecapFlowCategory> categories)
    {
        List<List<SpendingMapSlice>> panels = [];
        List<SpendingMapSlice> panel = [];
        var leaves = 0;
        foreach (var category in categories)
        {
            var part = 0;
            var parts = (category.Tags.Count + TagsPerPanel - 1) / TagsPerPanel;
            foreach (var tags in category.Tags.Chunk(TagsPerPanel))
            {
                if (leaves + tags.Length > TagsPerPanel || panel.Count == CategoriesPerPanel)
                {
                    panels.Add(panel);
                    panel = [];
                    leaves = 0;
                }

                panel.Add(new SpendingMapSlice(category, tags, ++part, parts));
                leaves += tags.Length;
            }
        }

        if (panel.Count > 0)
            panels.Add(panel);
        return panels;
    }

    public static float Thickness(long cents, long total, float mass) => (float)((double)cents / total * mass);

    public static float LeafHeight(RecapFlowTag tag, long total) =>
        Math.Max(37, Thickness(tag.TotalCents, total, FullMass) + 3);

    public static float GroupHeight(SpendingMapSlice slice, long total) =>
        Math.Max(slice.Tags.Sum(tag => LeafHeight(tag, total)), Thickness(slice.TotalCents, total, FullMass) + 42) + 10;

    public static (float Height, float AutomaticTop, float VariableTop) Layout(List<SpendingMapSlice> panel, long total)
    {
        var automatic = Thickness(panel.Sum(slice => slice.AutomaticCents), total, FullMass);
        var variable = Thickness(panel.Sum(slice => slice.VariableCents), total, FullMass);
        var autoBox = automatic > 0 ? Math.Max(38, automatic) : 0;
        var variableBox = variable > 0 ? Math.Max(38, variable) : 0;
        var gap = autoBox > 0 && variableBox > 0 ? 20 : 0;
        var height = Math.Max(panel.Sum(slice => GroupHeight(slice, total)), autoBox + variableBox + gap);
        var space = height - autoBox - variableBox - gap;
        var top = space * (gap > 0 ? .32f : .5f);
        var automaticTop = top + (autoBox - automatic) / 2;
        var variableTop = height - top - variableBox + (variableBox - variable) / 2;
        return (height, automaticTop, variableTop);
    }

    // A personal label such as "Affitto 740" can disclose an amount even without a currency symbol.
    public static string PrivateLabel(string name, string fallback) =>
        name.EnumerateRunes().Any(c => Rune.GetUnicodeCategory(c) is UnicodeCategory.CurrencySymbol or UnicodeCategory.DecimalDigitNumber)
            || CurrencyWords().IsMatch(name)
            ? fallback
            : name;

    [GeneratedRegex(@"(?<!\p{L})(?:euro|eur|usd|gbp|chf)(?!\p{L})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CurrencyWords();
}
