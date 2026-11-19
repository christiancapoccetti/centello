using System.Reflection;
using System.Text.RegularExpressions;

namespace Centesimo.Application.Tests;

public sealed class MaterialSymbols_should_expected_behavior
{
    [Fact]
    public void Should_draw_every_glyph_with_the_bundled_font()
    {
        var mapped = FontFiles.MappedCodepoints(FontFiles.ReadIconFont());

        var missing = Glyphs()
            .Where(glyph => !mapped.Contains(char.ConvertToUtf32(glyph.Value, 0)))
            .Select(glyph => glyph.Name)
            .ToList();

        Assert.True(missing.Count == 0,
            $"{FontFiles.IconFontRelativePath} has no glyph for: {string.Join(", ", missing)}. " +
            $"Fetch a TTF src from {SubsetUrl()} with a non-browser User-Agent, replace the font file and rerun.");
    }

    [Fact]
    public void Should_ship_a_static_truetype_font()
    {
        var font = FontFiles.ReadIconFont();
        var tables = FontFiles.Tables(font);

        Assert.Equal(0x00010000u, FontFiles.SfntVersion(font));
        Assert.Contains("glyf", tables);
        Assert.DoesNotContain("fvar", tables);
    }

    [Fact]
    public void Should_give_each_glyph_its_own_private_use_character()
    {
        var glyphs = Glyphs();

        Assert.All(glyphs, glyph =>
        {
            Assert.Equal(1, glyph.Value.Length);
            Assert.InRange(glyph.Value[0], '', '');
        });
        Assert.Equal(glyphs.Count, glyphs.Select(glyph => glyph.Value).Distinct().Count());
    }

    private static IReadOnlyList<(string Name, string Value)> Glyphs() => typeof(MaterialSymbols)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Where(field => field.IsLiteral)
        .Select(field => (field.Name, (string)field.GetRawConstantValue()!))
        .ToList();

    private static string SubsetUrl()
    {
        var names = Glyphs()
            .Select(glyph => Regex.Replace(glyph.Name, "(?<!^)([A-Z])", "_$1").ToLowerInvariant())
            .Order(StringComparer.Ordinal);
        return "https://fonts.googleapis.com/css2?family=Material+Symbols+Rounded:opsz,wght,FILL,GRAD@24,500,1,0&icon_names="
            + string.Join(",", names);
    }
}
