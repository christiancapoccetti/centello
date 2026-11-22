using Centello.App.Controls;

namespace Centello.Application.Tests;

public sealed class PhoneLandscapeSize_should_expected_behavior
{
    [Theory]
    [InlineData(560, 300, true)]
    [InlineData(852, 390, true)]
    [InlineData(600, 599, true)]
    [InlineData(559, 320, false)]
    [InlineData(390, 852, false)]
    [InlineData(560, 560, false)]
    [InlineData(1024, 600, false)]
    [InlineData(720, 0, false)]
    public void Use_the_phone_landscape_layout_for_wide_short_windows(double width, double height, bool expected) =>
        Assert.Equal(expected, PhoneLandscapeSize.Matches(width, height));
}
