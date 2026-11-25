namespace Centello.Application.Tests;

public sealed class FeedbackForm_should_expected_behavior
{
    [Fact]
    public void Use_the_activated_destination_without_any_ai_settings()
    {
        var form = new FeedbackForm();
        const string link = "https://docs.google.com/forms/d/e/example/viewform?usp=sharing";
        Assert.Null(form.Url);

        form.Apply(link);

        Assert.Equal(new Uri(link), form.Url);
    }

    [Theory]
    [InlineData("")]
    [InlineData("relative")]
    [InlineData("http://forms.gle/example")]
    [InlineData("javascript:alert(1)")]
    [InlineData("https://")]
    public void Hide_a_previously_available_form_when_configuration_is_removed_or_invalid(string value)
    {
        var form = new FeedbackForm();
        var changes = 0;
        form.Changed += () => changes++;
        form.Apply("https://forms.gle/example");
        form.Apply("https://forms.gle/example");

        form.Apply(value);

        Assert.Null(form.Url);
        Assert.Equal(2, changes);
    }

    [Fact]
    public void Observe_a_change_to_the_form_destination()
    {
        var form = new FeedbackForm();
        form.Apply("https://forms.gle/first");
        Uri? observed = null;
        form.Changed += () => observed = form.Url;
        form.Apply("https://forms.gle/second");

        Assert.Equal(new Uri("https://forms.gle/second"), observed);
    }
}
