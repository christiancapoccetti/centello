namespace Centello.Application.Tests;

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
        var components = ReadAppNotices();

        Assert.Equal(
            [".NET runtime, .NET for Android, .NET MAUI, Entity Framework Core and Microsoft.Data.Sqlite", "SQLite", "SQLitePCLRaw",
                "AndroidX, including DataStore, and Material Components for Android", "Glide", "Glide DiskLruCache", "Glide GIF decoder",
                "Gson, Tink, Guava ListenableFuture and Error Prone annotations", "Protobuf Lite (shaded in Tink Android and AndroidX DataStore)",
                "Kotlin standard library, kotlinx.coroutines, kotlinx.io, kotlinx.serialization and JetBrains annotations",
                "ThreeTenBP (code in the Kotlin standard library)", "Boost math functions (code in the Kotlin standard library)",
                "JSpecify and JSR 305 annotations",
                "Firebase AI Logic (18.2.0), Firebase App Check (20.1.0), Firebase Common, Components and Annotations",
                "Google Play services Base and Basement (18.9.0), Tasks (18.4.0), and Firebase Auth Interop (18.0.0)",
                "Google Play Integrity (1.4.0, Play variant)", "Google Play Core Common (2.0.4, Play variant)",
                "Ktor (3.2.4)", "OkHttp (4.12.0) and Okio (3.12.0)", "javax.inject (1)",
                "SLF4J API and NOP provider (2.0.17)", "Reactive Streams (1.0.4)", "Archivo fonts (Regular, SemiBold, Bold, Condensed ExtraBold)",
                "Material Symbols Rounded icon font (static subset)"],
            components.Select(component => component.Name));
        Assert.Equal(
            ["MIT License", "Pubblico dominio", "Apache License 2.0", "Apache License 2.0", "BSD 2-Clause License", "Apache License 2.0",
                "MIT License", "Apache License 2.0", "BSD-3-Clause (Protobuf Lite)", "Apache License 2.0", "BSD-3-Clause (ThreeTenBP)",
                "Boost Software License 1.0", "Apache License 2.0", "Apache License 2.0", "Android Software Development Kit License",
                "Play Integrity API Terms of Service", "Play Core Software Development Kit Terms of Service", "Apache License 2.0",
                "Apache License 2.0", "Apache License 2.0", "MIT License", "MIT No Attribution", "SIL Open Font License 1.1", "Apache License 2.0"],
            components.Select(component => component.License));
        Assert.Contains("Permission is hereby granted, free of charge", components[0].Text);
        Assert.DoesNotContain("\nPermission is hereby granted", components[1].Text);
        Assert.Contains("Redistributions in binary form must reproduce the above copyright notice", components[4].Text);
        Assert.Contains("DISCLAIMER THE FONT SOFTWARE IS PROVIDED", Assert.Single(components, component => component.License == "SIL Open Font License 1.1").Text);
    }

    [Fact]
    public void Keep_the_distinct_bsd_3_clause_notices_with_their_components()
    {
        var components = ReadAppNotices();
        var protobuf = Assert.Single(components, component => component.Name == "Protobuf Lite (shaded in Tink Android and AndroidX DataStore)");
        var threeTen = Assert.Single(components, component => component.Name == "ThreeTenBP (code in the Kotlin standard library)");

        Assert.Contains("Copyright 2008 Google Inc.", protobuf.Text);
        Assert.Contains("Neither the name of Google Inc. nor the names of its contributors", protobuf.Text);
        Assert.EndsWith("This support library is itself covered by the above license.", protobuf.Text);
        Assert.Contains("Copyright (c) 2007-present, Stephen Colebourne & Michael Nascimento Santos.", threeTen.Text);
        Assert.Contains("Neither the name of JSR-310 nor the names of its contributors", threeTen.Text);
        Assert.EndsWith("SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.", threeTen.Text);
        Assert.DoesNotContain("Copyright 2008 Google Inc.", threeTen.Text);
        Assert.DoesNotContain("Stephen Colebourne", protobuf.Text);
    }

    [Fact]
    public void Show_the_complete_glide_terms()
    {
        var glide = Assert.Single(ReadAppNotices(), component => component.Name == "Glide");

        Assert.Contains("Copyright 2014 Google, Inc. All rights reserved.", glide.Text);
        Assert.Contains("THIS SOFTWARE IS PROVIDED BY GOOGLE, INC.", glide.Text);
        Assert.EndsWith("The views and conclusions contained in the software and documentation are those of the authors and should not be interpreted as representing official policies, either expressed or implied, of Google, Inc.", glide.Text);
    }

    [Fact]
    public void Show_the_boost_attribution_and_object_code_exception()
    {
        var boost = Assert.Single(ReadAppNotices(), component => component.Name == "Boost math functions (code in the Kotlin standard library)");

        Assert.Contains("Copyright Eric Ford & Hubert Holin 2001.", boost.Text);
        Assert.Contains("asinh, acosh and atanh", boost.Text);
        Assert.Contains("unless such copies or derivative works are solely in the form of machine-executable object code generated by a source language processor.", boost.Text);
        Assert.EndsWith("DEALINGS IN THE SOFTWARE.", boost.Text);
    }

    [Fact]
    public void Ship_the_whole_apache_license_with_the_components_under_it()
    {
        var apache = ReadAppNotices()
            .Where(component => component.License == "Apache License 2.0")
            .ToList();

        Assert.NotEmpty(apache);
        Assert.All(apache, component =>
        {
            Assert.Contains("You must give any other recipients of the Work or Derivative Works a copy of this License", component.Text);
            Assert.Contains("END OF TERMS AND CONDITIONS", component.Text);
        });
    }

    [Theory]
    [InlineData("DOTNET_RUNTIME_THIRD_PARTY_NOTICES.txt", ".NET Runtime uses third-party libraries")]
    [InlineData("DOTNET_ANDROID_THIRD_PARTY_NOTICES.txt", "THIRD - PARTY SOFTWARE NOTICES AND INFORMATION")]
    public void Ship_the_notices_of_the_dotnet_runtimes(string asset, string expected)
    {
        var text = File.ReadAllText(Path.Combine(FontFiles.RepoRoot(), "src/Centello.App/Resources/Raw", asset));

        Assert.Contains(expected, ThirdPartyNotices.Upstream("Runtime", text).Text);
    }

    [Fact]
    public void Keep_the_layout_of_an_upstream_notices_file()
    {
        var component = ThirdPartyNotices.Upstream("Runtime .NET", "\r\nNotice for A\r\n-----\r\n\r\nCopyright A.\r\n");

        Assert.Equal(("Runtime .NET", "Licenze varie", "Notice for A\n-----\n\nCopyright A."), (component.Name, component.License, component.Text));
    }

    [Fact]
    public void Reflow_a_wrapped_text_into_paragraphs()
    {
        var text = "  GNU GENERAL PUBLIC LICENSE\r\n     Version 3\r\n\r\nThe licenses for most\r\nsoftware are\r\n   designed to take away.\r\n";

        Assert.Equal("GNU GENERAL PUBLIC LICENSE Version 3\n\nThe licenses for most software are designed to take away.", LicenseText.Reflow(text));
    }

    [Fact]
    public void Show_a_markdown_license_as_plain_paragraphs()
    {
        var markdown = "Required Notice: Copyright A (https://a.example)\r\n\r\n# Terms 1.0\r\n\r\n<https://terms.example/1.0>\r\n\r\n## Notices\r\n\r\nSee [Distribution](#distribution) and lines beginning with `Notice:`.\r\n\r\n> Notice: Copyright B\r\n\r\n***As is, without warranty.*** The **licensor** is you.\r\n";

        Assert.Equal(
            "Required Notice: Copyright A (https://a.example)\n\nTerms 1.0\n\nhttps://terms.example/1.0\n\nNotices\n\nSee Distribution and lines beginning with Notice:.\n\nNotice: Copyright B\n\nAs is, without warranty. The licensor is you.",
            LicenseText.FromMarkdown(markdown));
    }

    private static IReadOnlyList<NoticeComponent> ReadAppNotices() =>
        ThirdPartyNotices.Parse(File.ReadAllText(Path.Combine(FontFiles.RepoRoot(), "src/Centello.App/Resources/Raw/THIRD_PARTY_NOTICES.txt")));
}
