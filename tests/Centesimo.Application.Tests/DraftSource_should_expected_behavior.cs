namespace Centesimo.Application.Tests;

public sealed class DraftSource_should_expected_behavior
{
    [Fact]
    public void Quote_what_the_voice_heard_in_the_banner()
    {
        var banner = DraftSource.Voice.Banner("Ho speso 100 euro di carburante");

        Assert.Equal("Dalla voce: “Ho speso 100 euro di carburante” · controlla e salva", banner);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Show_no_voice_banner_when_nothing_was_heard(string heard) =>
        Assert.Equal("", DraftSource.Voice.Banner(heard));

    [Theory]
    [InlineData("")]
    [InlineData("Ho speso 100 euro")]
    public void Show_the_same_photo_banner_whatever_was_heard(string heard) =>
        Assert.Equal("Dalla foto · controlla e salva", DraftSource.Photo.Banner(heard));

    [Fact]
    public void Speak_for_the_voice_on_the_sheet()
    {
        Assert.Equal(MaterialSymbols.Mic, DraftSource.Voice.Glyph);
        Assert.Equal("Inserimento vocale", DraftSource.Voice.SheetTitle);
        Assert.Equal("", DraftSource.Voice.ReadingLine);
        Assert.Equal("Audio e testo inviati a OpenAI", DraftSource.Voice.PrivacyNote);
    }

    [Fact]
    public void Speak_for_the_photo_on_the_sheet()
    {
        Assert.Equal(MaterialSymbols.PhotoCamera, DraftSource.Photo.Glyph);
        Assert.Equal("Lettura della foto", DraftSource.Photo.SheetTitle);
        Assert.Equal("Cerco importo, data e negozio…", DraftSource.Photo.ReadingLine);
        Assert.Equal("Foto inviata a OpenAI, Centesimo non la salva", DraftSource.Photo.PrivacyNote);
    }
}
