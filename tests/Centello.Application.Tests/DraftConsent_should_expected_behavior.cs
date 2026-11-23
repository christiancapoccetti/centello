namespace Centello.Application.Tests;

public sealed class DraftConsent_should_expected_behavior
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(DraftConsent.CurrentVersion + 1)]
    public void Require_acceptance_of_the_current_disclosure(int storedVersion)
    {
        var consent = new DraftConsent(new FakeDraftConsentStore { AcceptedVersion = storedVersion });

        Assert.False(consent.IsEnabled);
    }

    [Fact]
    public void Persist_opt_in_and_revoke_it()
    {
        var store = new FakeDraftConsentStore { AcceptedVersion = 0 };
        var consent = new DraftConsent(store);

        consent.SetEnabled(true);

        Assert.Equal(DraftConsent.CurrentVersion, store.AcceptedVersion);
        Assert.True(new DraftConsent(store).IsEnabled);

        consent.SetEnabled(false);

        Assert.False(new DraftConsent(store).IsEnabled);
    }
}
