using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Centesimo.Application.Tests;

public sealed class AndroidBackupRules_should_expected_behavior
{
    private const string AndroidRoot = "src/Centesimo.App/Platforms/Android";
    private static readonly XNamespace Android = "http://schemas.android.com/apk/res/android";

    [Fact]
    public void Manifest_turns_backup_on_and_points_at_both_rule_files()
    {
        var application = Load("AndroidManifest.xml").Root!.Element("application")!;

        Assert.Equal("true", (string?)application.Attribute(Android + "allowBackup"));
        Assert.Equal("@xml/backup_rules", (string?)application.Attribute(Android + "fullBackupContent"));
        Assert.Equal("@xml/data_extraction_rules", (string?)application.Attribute(Android + "dataExtractionRules"));
    }

    [Fact]
    public void Every_backup_section_includes_exactly_the_same_entries()
    {
        var fullBackup = Includes(LoadRules("backup_rules.xml"));
        var rules = LoadRules("data_extraction_rules.xml");

        Assert.NotEmpty(fullBackup);
        Assert.Equal(fullBackup, Includes(rules.Element("cloud-backup")!));
        Assert.Equal(fullBackup, Includes(rules.Element("device-transfer")!));
    }

    [Fact]
    public void Backup_includes_only_the_database_and_its_wal_in_the_file_domain()
    {
        var databaseName = DatabaseFileName();
        var expected = new[] { ("file", databaseName), ("file", databaseName + "-wal") };

        Assert.Equal(expected, Includes(LoadRules("backup_rules.xml")));
        Assert.Equal(expected, Includes(LoadRules("data_extraction_rules.xml").Element("cloud-backup")!));
    }

    [Fact]
    public void Backup_rules_have_no_excludes()
    {
        var sections = new[]
        {
            LoadRules("backup_rules.xml"),
            LoadRules("data_extraction_rules.xml").Element("cloud-backup")!,
            LoadRules("data_extraction_rules.xml").Element("device-transfer")!,
        };

        Assert.All(sections, section => Assert.Empty(section.Elements("exclude")));
    }

    private static (string Domain, string Path)[] Includes(XElement section) =>
        section
            .Elements("include")
            .Select(include => ((string)include.Attribute("domain")!, (string)include.Attribute("path")!))
            .OrderBy(include => include.Item2, StringComparer.Ordinal)
            .ToArray();

    private static XElement LoadRules(string fileName) => Load(Path.Combine("Resources", "xml", fileName)).Root!;

    private static XDocument Load(string relativePath) => XDocument.Load(Path.Combine(FontFiles.RepoRoot(), AndroidRoot, relativePath));

    private static string DatabaseFileName()
    {
        var source = File.ReadAllText(Path.Combine(FontFiles.RepoRoot(), "src/Centesimo.App/MauiProgram.cs"));
        var match = Regex.Match(source, """"([^"]+\.db3)"""");
        Assert.True(match.Success, "MauiProgram.cs has no .db3 literal.");
        return match.Groups[1].Value;
    }
}
