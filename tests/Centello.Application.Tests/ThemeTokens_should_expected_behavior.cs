using System.Text;
using System.Text.RegularExpressions;

namespace Centello.Application.Tests;

public class ThemeTokens_should_expected_behavior
{
    private const string ResourcesPath = "src/Centello.App/Platforms/Android/Resources";

    [Fact]
    public void Carry_every_token_in_the_light_resource_file() =>
        AssertFileEquals("values", token => token.Light);

    [Fact]
    public void Carry_every_token_in_the_dark_resource_file() =>
        AssertFileEquals("values-night", token => token.Dark);

    [Fact]
    public void Name_each_resource_after_its_token_in_snake_case()
    {
        Assert.Equal("ground", ResourceName(ThemeColor.Ground));
        Assert.Equal("surface_raised", ResourceName(ThemeColor.SurfaceRaised));
        Assert.Equal("on_positive_container", ResourceName(ThemeColor.OnPositiveContainer));
    }

    private static void AssertFileEquals(string folder, Func<(string Dark, string Light), string> pick)
    {
        var path = Path.Combine(FontFiles.RepoRoot(), ResourcesPath, folder, "theme_colors.xml");
        var expected = ExpectedXml(pick);
        var actual = File.Exists(path) ? Normalize(File.ReadAllText(path)) : "";

        Assert.True(expected == actual, $"{path} must contain exactly:\n{expected}");
    }

    private static string ExpectedXml(Func<(string Dark, string Light), string> pick)
    {
        var xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n<resources>\n");
        foreach (var color in Enum.GetValues<ThemeColor>())
            xml.Append($"    <color name=\"theme_{ResourceName(color)}\">{pick(ThemeTokens.Hex(color))}</color>\n");

        return xml.Append("</resources>\n").ToString();
    }

    private static string ResourceName(ThemeColor color) =>
        Regex.Replace(color.ToString(), "(?<=[a-z])(?=[A-Z])", "_").ToLowerInvariant();

    private static string Normalize(string text) => text.Replace("\r\n", "\n");
}
