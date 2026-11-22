namespace Centello.Application.Tests;

public sealed class RecapPress_should_expected_behavior
{
    private static TimeSpan Ms(int value) => TimeSpan.FromMilliseconds(value);

    [Fact]
    public void Should_be_down_between_the_press_and_the_release()
    {
        var press = new RecapPress();
        Assert.False(press.IsDown);

        press.Down(Ms(0));
        Assert.True(press.IsDown);

        press.Up(Ms(100));
        Assert.False(press.IsDown);
    }

    [Theory]
    [InlineData(80, false)]
    [InlineData(299, false)]
    [InlineData(300, true)]
    [InlineData(2000, true)]
    public void Should_call_a_press_a_hold_from_300_ms(int length, bool isHold)
    {
        var press = new RecapPress();

        press.Down(Ms(1000));
        press.Up(Ms(1000 + length));

        Assert.Equal(isHold, press.WasHold);
    }

    [Fact]
    public void Should_judge_each_press_on_its_own()
    {
        var press = new RecapPress();
        press.Down(Ms(0));
        press.Up(Ms(900));

        press.Down(Ms(2000));
        press.Up(Ms(2100));

        Assert.False(press.WasHold);
    }

    [Fact]
    public void Should_ignore_a_release_without_a_press()
    {
        var press = new RecapPress();

        press.Up(Ms(5000));

        Assert.False(press.WasHold);
    }
}
