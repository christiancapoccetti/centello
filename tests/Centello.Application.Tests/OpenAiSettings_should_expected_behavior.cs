namespace Centello.Application.Tests;

public sealed class OpenAiSettings_should_expected_behavior
{
    private readonly FakeOpenAiApiKeyStore _store = new();
    private readonly OpenAiSettings _settings;

    public OpenAiSettings_should_expected_behavior() => _settings = new OpenAiSettings(_store);

    [Fact]
    public void Tell_the_user_that_expense_photos_and_pdfs_are_sent_to_OpenAI()
    {
        Assert.Contains("le foto e i PDF delle spese", OpenAiSettings.ConsentMessage);
        Assert.Contains("non salva", OpenAiSettings.ConsentMessage);
    }

    [Fact]
    public async Task Have_no_key_until_one_is_saved()
    {
        Assert.False(await _settings.HasApiKey());
        Assert.Equal("", await _settings.GetApiKey());
    }

    [Fact]
    public async Task Store_the_trimmed_key_when_the_user_consents()
    {
        var result = await _settings.SaveApiKey("  sk-test \n", consentAccepted: true);

        Assert.True(result.IsSuccess);
        Assert.True(await _settings.HasApiKey());
        Assert.Equal("sk-test", await _settings.GetApiKey());
    }

    [Fact]
    public async Task Refuse_a_key_without_consent_and_keep_the_stored_one()
    {
        await _settings.SaveApiKey("sk-test", consentAccepted: true);

        var result = await _settings.SaveApiKey("sk-other", consentAccepted: false);

        Assert.True(result.IsFailure);
        Assert.Equal(DraftErrors.ConsentRequired, result.Error);
        Assert.Equal("sk-test", await _settings.GetApiKey());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Reject_a_blank_key_and_keep_the_stored_one(string key)
    {
        await _settings.SaveApiKey("sk-test", consentAccepted: true);

        var result = await _settings.SaveApiKey(key, consentAccepted: true);

        Assert.True(result.IsFailure);
        Assert.Equal(DraftErrors.EmptyApiKey, result.Error);
        Assert.Equal("sk-test", await _settings.GetApiKey());
    }

    [Fact]
    public async Task Remove_the_key_when_it_is_cleared()
    {
        await _settings.SaveApiKey("sk-test", consentAccepted: true);

        _settings.ClearApiKey();

        Assert.False(await _settings.HasApiKey());
    }

    private sealed class FakeOpenAiApiKeyStore : IOpenAiApiKeyStore
    {
        private string _apiKey = "";

        public Task<string> GetApiKey() => Task.FromResult(_apiKey);

        public Task SetApiKey(string apiKey)
        {
            _apiKey = apiKey;
            return Task.CompletedTask;
        }

        public void ClearApiKey() => _apiKey = "";
    }
}
