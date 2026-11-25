using Centello.App;

namespace Centello.Application.Tests;

public sealed class FirebaseAiSettings_should_expected_behavior
{
    [Fact]
    public void Read_remote_ids_and_the_optional_survey_url()
    {
        const string id = "11111111111111111111111111111111";
        const string survey = "https://forms.gle/example";

        var settings = FirebaseAiSettings.Parse(survey, $"[\"{id}\"]");

        Assert.Equal(survey, settings?.SurveyUrl);
        Assert.Contains(id, settings!.RevokedInviteIds);
    }

    [Fact]
    public void Accept_an_explicitly_empty_list_without_a_survey_url()
    {
        var settings = FirebaseAiSettings.Parse(null, "[]");

        Assert.Equal("", settings?.SurveyUrl);
        Assert.Empty(settings!.RevokedInviteIds);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("null")]
    [InlineData("{}")]
    [InlineData("[")]
    [InlineData("[1]")]
    [InlineData("[\"unknown\"]")]
    [InlineData("[\"\\ud800\"]")]
    [InlineData("[\"AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA\"]")]
    [InlineData("[\"11111111111111111111111111111111\", null]")]
    public void Reject_a_missing_or_malformed_revocation_list(string? list)
    {
        Assert.Null(FirebaseAiSettings.Parse("https://forms.gle/example", list));
    }
}
