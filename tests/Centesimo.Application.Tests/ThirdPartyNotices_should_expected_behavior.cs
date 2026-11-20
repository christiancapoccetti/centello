namespace Centesimo.Application.Tests;

public sealed class ThirdPartyNotices_should_expected_behavior
{
    private const string Notices = """
        TITLE

        Intro line.

        Alpha and Beta
        Copyright (c) Alpha.
        License: MIT License.
        https://example.com/alpha

        Gamma
        Gamma is in the public domain.
        https://example.com/gamma

        Delta font
        Copyright Delta.
        License: SIL Open Font License 1.1. The full text is at the end of this file.

        ----------
        MIT License
        ----------

        Permission is hereby granted,
        free of charge.

        "License" shall mean the terms,
        as defined here.

        THE SOFTWARE IS PROVIDED "AS IS".

        -----------------------------------------------------------
        SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007
        -----------------------------------------------------------

        PREAMBLE
        The goals are
        simple.
        """;

    [Fact]
    public void List_one_component_per_paragraph_with_its_short_license()
    {
        var components = ThirdPartyNotices.Parse(Notices);

        Assert.Equal(
            [("Alpha and Beta", "MIT License"), ("Gamma", "Pubblico dominio"), ("Delta font", "SIL Open Font License 1.1")],
            components.Select(component => (component.Name, component.License)));
    }

    [Fact]
    public void Add_the_full_text_of_the_license_a_component_names()
    {
        var components = ThirdPartyNotices.Parse(Notices);

        Assert.Equal(
            "Copyright (c) Alpha.\nLicense: MIT License.\nhttps://example.com/alpha\n\n" +
            "MIT License\n\nPermission is hereby granted, free of charge.\n\n\"License\" shall mean the terms, as defined here.\n\n" +
            "THE SOFTWARE IS PROVIDED \"AS IS\".",
            components[0].Text);
        Assert.EndsWith("SIL OPEN FONT LICENSE Version 1.1 - 26 February 2007\n\nPREAMBLE The goals are simple.", components[2].Text);
    }

    [Fact]
    public void Keep_only_the_notice_of_a_component_without_a_license_text()
    {
        var gamma = ThirdPartyNotices.Parse(Notices)[1];

        Assert.Equal("Gamma is in the public domain.\nhttps://example.com/gamma", gamma.Text);
    }

    [Fact]
    public void List_nothing_for_a_text_without_components() =>
        Assert.Empty(ThirdPartyNotices.Parse("Just a sentence."));

    [Fact]
    public void Read_the_notices_the_app_ships_with()
    {
        var text = File.ReadAllText(Path.Combine(FontFiles.RepoRoot(), "src/Centesimo.App/Resources/Raw/THIRD_PARTY_NOTICES.txt"));

        var components = ThirdPartyNotices.Parse(text);

        Assert.Equal(
            [".NET runtime, .NET for Android, .NET MAUI, Entity Framework Core and Microsoft.Data.Sqlite", "SQLite", "SQLitePCLRaw",
                "AndroidX and Material Components for Android", "Glide", "Glide DiskLruCache", "Glide GIF decoder",
                "Gson, Tink, Guava ListenableFuture and Error Prone annotations",
                "Kotlin standard library, kotlinx.coroutines, kotlinx.serialization and JetBrains annotations",
                "JSpecify and JSR 305 annotations", "Archivo fonts (Regular, SemiBold, Bold, Condensed ExtraBold)",
                "Material Symbols Rounded icon font (static subset)"],
            components.Select(component => component.Name));
        Assert.Equal(
            ["MIT License", "Pubblico dominio", "Apache License 2.0", "Apache License 2.0", "BSD 2-Clause License", "Apache License 2.0",
                "MIT License", "Apache License 2.0", "Apache License 2.0", "Apache License 2.0", "SIL Open Font License 1.1", "Apache License 2.0"],
            components.Select(component => component.License));
        Assert.Contains("Permission is hereby granted, free of charge", components[0].Text);
        Assert.DoesNotContain("\nPermission is hereby granted", components[1].Text);
        Assert.Contains("Redistributions in binary form must reproduce the above copyright notice", components[4].Text);
        Assert.Contains("DISCLAIMER THE FONT SOFTWARE IS PROVIDED", components[10].Text);
    }

    [Fact]
    public void Ship_the_whole_apache_license_with_the_components_under_it()
    {
        var text = File.ReadAllText(Path.Combine(FontFiles.RepoRoot(), "src/Centesimo.App/Resources/Raw/THIRD_PARTY_NOTICES.txt"));

        var apache = ThirdPartyNotices.Parse(text).Where(component => component.License == "Apache License 2.0").ToList();

        Assert.NotEmpty(apache);
        Assert.All(apache, component =>
        {
            Assert.Contains("You must give any other recipients of the Work or Derivative Works a copy of this License", component.Text);
            Assert.Contains("END OF TERMS AND CONDITIONS", component.Text);
        });
    }


    [Fact]
    public void Reflow_a_wrapped_text_into_paragraphs()
    {
        var text = "  GNU GENERAL PUBLIC LICENSE\r\n     Version 3\r\n\r\nThe licenses for most\r\nsoftware are\r\n   designed to take away.\r\n";

        Assert.Equal("GNU GENERAL PUBLIC LICENSE Version 3\n\nThe licenses for most software are designed to take away.", LicenseText.Reflow(text));
    }
}
