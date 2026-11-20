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

    [Fact]
    public void Show_the_pdf_banner_whatever_was_heard() =>
        Assert.Equal("Dal PDF · controlla e salva", DraftSource.Pdf.Banner("Ho speso 100 euro"));

    [Fact]
    public void Speak_for_the_pdf_on_the_sheet()
    {
        Assert.Equal(MaterialSymbols.PictureAsPdf, DraftSource.Pdf.Glyph);
        Assert.Equal("Lettura del PDF", DraftSource.Pdf.SheetTitle);
        Assert.Equal(DraftSource.Photo.ReadingLine, DraftSource.Pdf.ReadingLine);
        Assert.Equal("PDF inviato a OpenAI, Centesimo non lo salva", DraftSource.Pdf.PrivacyNote);
    }

    [Fact]
    public void Take_the_source_from_the_kind_of_document()
    {
        Assert.Same(DraftSource.Photo, DraftSource.Of(ExpenseDocument.Read(new byte[] { 0xFF, 0xD8, 0xFF }).Value));
        Assert.Same(DraftSource.Pdf, DraftSource.Of(ExpenseDocument.Read("%PDF-1.7"u8.ToArray()).Value));
    }
}
