namespace Centello.Application.Tests;

public sealed class OpenAiSettings_should_expected_behavior
{
    private readonly FakeOpenAiApiKeyStore _store = new();
    private readonly OpenAiSettings _settings;

    public OpenAiSettings_should_expected_behavior() => _settings = new OpenAiSettings(_store);

    [Theory]
    [InlineData("l'audio registrato")]
    [InlineData("il testo trascritto")]
    [InlineData("la foto o il PDF")]
    [InlineData("nomi delle categorie e dei tag attivi")]
    [InlineData("la data di oggi")]
    public void Disclose_what_is_sent_to_OpenAI(string disclosedData)
    {
        Assert.Contains("OpenAI", OpenAiSettings.ConsentMessage);
        Assert.Contains(disclosedData, OpenAiSettings.ConsentMessage);
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
        Assert.Equal(OpenAiApiKeyStatus.Active, await _settings.GetApiKeyStatus());
        Assert.Equal("sk-test", await _settings.GetApiKey());
        Assert.Equal(1, _store.Writes);
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

    [Fact]
    public async Task Withhold_an_existing_key_until_the_updated_disclosure_is_accepted()
    {
        _store.Key = new StoredOpenAiApiKey("sk-legacy", consentVersion: 0);

        IOpenAiApiKeyProvider provider = _settings;
        Assert.Equal(OpenAiApiKeyStatus.ConsentRequired, await _settings.GetApiKeyStatus());
        Assert.False(await _settings.HasApiKey());
        Assert.Equal("", await provider.GetApiKey());

        Assert.True((await _settings.AuthorizeStoredApiKey(consentAccepted: true)).IsSuccess);

        var reopened = new OpenAiSettings(_store);
        Assert.Equal(OpenAiApiKeyStatus.Active, await reopened.GetApiKeyStatus());
        Assert.Equal("sk-legacy", await reopened.GetApiKey());
        Assert.True((await reopened.AuthorizeStoredApiKey(consentAccepted: true)).IsSuccess);
        Assert.Equal(1, _store.Writes);
    }

    [Fact]
    public async Task Keep_a_legacy_key_unavailable_when_the_user_cancels()
    {
        var original = new StoredOpenAiApiKey("sk-legacy", consentVersion: 0);
        _store.Key = original;

        var result = await _settings.AuthorizeStoredApiKey(consentAccepted: false);

        Assert.Equal(DraftErrors.ConsentRequired, result.Error);
        Assert.Same(original, _store.Key);
        Assert.Equal(0, _store.Writes);
        Assert.Equal("", await _settings.GetApiKey());
    }

    [Fact]
    public async Task Allow_removing_a_key_before_reauthorization()
    {
        _store.Key = new StoredOpenAiApiKey("sk-legacy", consentVersion: 0);

        _settings.ClearApiKey();

        Assert.Equal(OpenAiApiKeyStatus.Missing, await _settings.GetApiKeyStatus());
        Assert.Equal(DraftErrors.MissingApiKey, (await _settings.AuthorizeStoredApiKey(consentAccepted: true)).Error);
    }

    private sealed class FakeOpenAiApiKeyStore : IOpenAiApiKeyStore
    {
        public StoredOpenAiApiKey? Key { get; set; }
        public int Writes { get; private set; }

        public Task<StoredOpenAiApiKey?> GetApiKey() => Task.FromResult(Key);

        public Task SetApiKey(StoredOpenAiApiKey apiKey)
        {
            Key = apiKey;
            Writes++;
            return Task.CompletedTask;
        }

        public void ClearApiKey() => Key = null;
    }
}
