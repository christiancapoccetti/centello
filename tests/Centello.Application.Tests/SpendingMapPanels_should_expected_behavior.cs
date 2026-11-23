using Centello.App.Controls.Recap;

namespace Centello.Application.Tests;

public sealed class SpendingMapPanels_should_expected_behavior
{
    [Fact]
    public void preserve_every_tag_and_cent_when_a_large_category_continues_in_another_panel()
    {
        var tags = Enumerable.Range(1, 97)
            .Select(i => new RecapFlowTag(Guid.NewGuid(), $"Tag {i}", i, i * 3))
            .ToArray();
        var category = new RecapFlowCategory(new RecapCategory(Guid.NewGuid(), "Casa", "home", "peri"), tags);
        var panels = SpendingMapPanels.Pack([category]);
        var slices = panels.SelectMany(panel => panel).ToArray();

        Assert.True(panels.Count > 1);
        Assert.Equal(tags, slices.SelectMany(slice => slice.Tags));
        Assert.Equal(category.TotalCents, slices.Sum(slice => slice.TotalCents));
        Assert.All(slices, slice => Assert.Equal(category, slice.Category));
        Assert.Equal(Enumerable.Range(1, 7), slices.Select(slice => slice.Part));
        Assert.All(slices, slice => Assert.Equal(7, slice.Parts));
    }

    [Theory]
    [InlineData(9900, 0, 0, 100)]
    [InlineData(0, 9900, 100, 0)]
    [InlineData(9900, 100, 0, 0)]
    [InlineData(9900, 0, 100, 0)]
    public void fit_source_labels_and_category_bands_when_a_single_tag_dominates(
        long firstAuto, long firstVariable, long nextAuto, long nextVariable)
    {
        var groups = new[] { (firstAuto, firstVariable), (nextAuto, nextVariable) }
            .Where(pair => pair.Item1 + pair.Item2 > 0)
            .Select(pair => new RecapFlowCategory(new RecapCategory(Guid.NewGuid(), "Casa", "home", "peri"),
                [new RecapFlowTag(Guid.NewGuid(), "Affitto", pair.Item1, pair.Item2)]))
            .ToList();
        var panel = Assert.Single(SpendingMapPanels.Pack(groups));
        var total = groups.Sum(group => group.TotalCents);
        var layout = SpendingMapPanels.Layout(panel, total);
        var auto = SpendingMapPanels.Thickness(firstAuto + nextAuto, total, SpendingMapPanels.FullMass);
        var variable = SpendingMapPanels.Thickness(firstVariable + nextVariable, total, SpendingMapPanels.FullMass);
        foreach (var (top, band) in new[] { (layout.AutomaticTop, auto), (layout.VariableTop, variable) })
        {
            if (band == 0)
                continue;
            var center = top + band / 2;
            Assert.InRange(top, 0, layout.Height - band + .001f);
            Assert.InRange(center, 17, layout.Height - 19 + .001f);
        }
        if (auto > 0 && variable > 0)
            Assert.True(layout.AutomaticTop + auto / 2 + 19 <= layout.VariableTop + variable / 2 - 17);

        foreach (var slice in panel)
        {
            var mass = SpendingMapPanels.Thickness(slice.TotalCents, total, SpendingMapPanels.FullMass);
            var contentHeight = SpendingMapPanels.GroupHeight(slice, total) - 10;
            Assert.True((contentHeight - mass) / 2 >= 20);
        }
    }

    [Theory]
    [InlineData("Affitto", "Affitto")]
    [InlineData("Linea 8", "Tag")]
    [InlineData("Affitto 740", "Tag")]
    [InlineData("Affitto ٧٤٠", "Tag")]
    [InlineData("Affitto 𝟟", "Tag")]
    [InlineData("Affitto 500 €", "Tag")]
    [InlineData("EUR 25", "Tag")]
    [InlineData("Abbonamento 30EUR", "Tag")]
    [InlineData("19,99 euro", "Tag")]
    public void redact_currency_markers_and_decimal_digits_in_private_labels(string name, string expected) =>
        Assert.Equal(expected, SpendingMapPanels.PrivateLabel(name, "Tag"));
}
