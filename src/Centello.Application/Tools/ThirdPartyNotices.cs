using System.Text.RegularExpressions;

namespace Centello.Application;

/// <summary>One third-party component as the licenses screen lists it. License is the short name ("MIT License"),
/// Text is what the person reads when the row is open: the notice of the component and the license text it names.</summary>
public sealed record NoticeComponent(string Name, string License, string Text);

/// <summary>Reads THIRD_PARTY_NOTICES.txt, the file that ships with the app: a title and an introduction, one
/// paragraph per component (its name on the first line, then the copyright, "License: ..." and a link), then the
/// full text of each license under a heading of its own, framed by rules of dashes.</summary>
public static class ThirdPartyNotices
{
    private const string LicensePrefix = "License:";
    private const string PublicDomain = "public domain";
    private const int HeadingNameWords = 3;
    private const string UpstreamLicenses = "Licenze varie";

    public static IReadOnlyList<NoticeComponent> Parse(string notices)
    {
        var paragraphs = Regex
            .Split(notices.Replace("\r\n", "\n").Trim(), @"\n\s*\n")
            .Select(paragraph => paragraph.Split('\n').Select(line => line.TrimEnd()).ToList())
            .ToList();
        var lastComponent = paragraphs.FindLastIndex(IsComponent);
        if (lastComponent < 0)
            return [];

        var licenses = LicenseTexts(paragraphs.Skip(lastComponent + 1));
        return paragraphs
            .Take(lastComponent + 1)
            .Where(IsComponent)
            .Select(lines => ToComponent(lines, licenses))
            .ToList();
    }

    /// <summary>A notices file a component ships for the parts it includes, as the .NET runtime does. It is shown as
    /// written, since its notices follow no layout of ours.</summary>
    public static NoticeComponent Upstream(string name, string notices) =>
        new(name, UpstreamLicenses, notices.Replace("\r\n", "\n").Trim());

    private static bool IsComponent(List<string> lines) =>
        lines.Count >= 2 && lines.Any(line => line.StartsWith(LicensePrefix) || line.Contains(PublicDomain, StringComparison.OrdinalIgnoreCase));

    private static NoticeComponent ToComponent(List<string> lines, IReadOnlyList<(string Heading, string Text)> licenses)
    {
        var license = ShortLicense(lines);
        var notice = string.Join("\n", lines.Skip(1));
        var full = licenses.FirstOrDefault(candidate => HeadingNames(candidate.Heading, license));
        return new NoticeComponent(lines[0], license, full.Text is null ? notice : $"{notice}\n\n{full.Text}");
    }

    private static string ShortLicense(List<string> lines)
    {
        var line = lines.FirstOrDefault(line => line.StartsWith(LicensePrefix));
        if (line is null)
            return "Pubblico dominio";

        // "License: SIL Open Font License 1.1. The full text is at the end of this file." ends at the first ". ".
        var name = line[LicensePrefix.Length..].Trim();
        var end = name.IndexOf(". ", StringComparison.Ordinal);
        return (end < 0 ? name : name[..end]).TrimEnd('.');
    }

    // A heading names a license when it starts with the first words of its name: "SIL OPEN FONT LICENSE Version 1.1"
    // names "SIL Open Font License 1.1".
    private static bool HeadingNames(string heading, string license)
    {
        var words = license.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(HeadingNameWords);
        return heading.StartsWith(string.Join(' ', words), StringComparison.OrdinalIgnoreCase);
    }

    private static List<(string Heading, string Text)> LicenseTexts(IEnumerable<List<string>> paragraphs)
    {
        var blocks = new List<(string Heading, List<string> Paragraphs)>();
        foreach (var paragraph in paragraphs)
        {
            var lines = paragraph.Select(line => line.Trim()).ToList();
            if (IsHeading(lines))
                blocks.Add((lines[1], [string.Join("\n", lines[1..^1])]));
            else if (blocks.Count > 0)
                blocks[^1].Paragraphs.Add(LicenseText.Reflow(string.Join("\n", lines)));
        }

        return blocks.Select(block => (block.Heading, string.Join("\n\n", block.Paragraphs))).ToList();
    }

    // A heading is the name of a license framed by two rules of dashes. The frame is what sets it apart: license texts
    // have short paragraphs of their own that start with the word "License".
    private static bool IsHeading(List<string> lines) => lines.Count >= 3 && IsRule(lines[0]) && IsRule(lines[^1]);

    private static bool IsRule(string line) => line.Length > 0 && line.All(character => character == '-');
}

/// <summary>The text of a license as the screen shows it.</summary>
public static class LicenseText
{
    /// <summary>Joins the lines a paragraph was wrapped over and drops the indentation, so the text fills the width of
    /// the screen instead of breaking where a terminal once did. Paragraphs stay apart.</summary>
    public static string Reflow(string text) => string.Join(
        "\n\n",
        Regex
            .Split(text.Replace("\r\n", "\n").Trim(), @"\n\s*\n")
            .Select(paragraph => Regex.Replace(paragraph, @"\s+", " ").Trim()));

    /// <summary>A license written in Markdown, as PolyForm publishes its terms, shown as plain paragraphs: headings,
    /// quotes, emphasis, code and links keep their words and lose their marks.</summary>
    public static string FromMarkdown(string markdown)
    {
        var text = Regex.Replace(markdown, @"^[ \t]*(#+|>)[ \t]?", "", RegexOptions.Multiline);
        text = Regex.Replace(text, @"\[([^\]]+)\]\([^)]*\)", "$1");
        text = Regex.Replace(text, @"<(https?://[^>]+)>", "$1");
        text = Regex.Replace(text, @"\*{1,3}([^*\n]+?)\*{1,3}", "$1");
        return Reflow(text.Replace("`", ""));
    }
}
