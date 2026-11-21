namespace Centesimo.Application.Tests;

public sealed class WaterfallLabels_should_expected_behavior
{
    private const char Minus = '−';

    // Six dp a character at 12sp, in proportion at the other sizes.
    private static double Width(string text, double size) => text.Length * 6 * size / 12;

    [Fact]
    public void Should_keep_a_label_that_fits_its_column_as_it_is() =>
        Assert.Equal(new WaterfallLabel("+524", 12), WaterfallLabels.Fit("+524", 36, Width));

    [Fact]
    public void Should_shrink_a_label_that_is_a_little_wider_than_its_column_and_keep_its_digits() =>
        Assert.Equal(new WaterfallLabel("+1.527", 11), WaterfallLabels.Fit("+1.527", 34, Width));

    [Fact]
    public void Should_shrink_to_the_smallest_size_before_it_gives_up_a_digit() =>
        Assert.Equal(new WaterfallLabel("+1.527", 10), WaterfallLabels.Fit("+1.527", 30, Width));

    [Fact]
    public void Should_write_thousands_with_a_mark_when_the_digits_do_not_fit_even_at_the_smallest_size() =>
        Assert.Equal(new WaterfallLabel("+1,5k", 11), WaterfallLabels.Fit("+1.527", 28, Width));

    [Fact]
    public void Should_fall_back_to_the_compact_form_at_the_smallest_size_when_nothing_fits() =>
        Assert.Equal(new WaterfallLabel("2,1M", 10), WaterfallLabels.Fit("2.131.824", 5, Width));

    [Theory]
    [InlineData("+1.527", "+1,5k")]
    [InlineData("1.527", "1,5k")]
    [InlineData("+9.999", "+10k")]
    [InlineData("12.400", "12k")]
    [InlineData("123.456", "123k")]
    [InlineData("999.999", "1M")]
    [InlineData("2.131.824", "2,1M")]
    [InlineData("12.500.000", "13M")]
    public void Should_compact_a_value_of_a_thousand_or_more(string value, string expected) =>
        Assert.Equal(expected, WaterfallLabels.Compact(value));

    [Fact]
    public void Should_keep_the_minus_sign_of_a_fall_when_it_compacts() =>
        Assert.Equal($"{Minus}12k", WaterfallLabels.Compact($"{Minus}12.400"));

    [Theory]
    [InlineData("+524")]
    [InlineData("0")]
    [InlineData("999")]
    public void Should_not_compact_a_value_under_a_thousand(string value) =>
        Assert.Equal(value, WaterfallLabels.Compact(value));
}
