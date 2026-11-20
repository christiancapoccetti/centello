using Centesimo.Domain;

namespace Centesimo.Application;

public sealed class DraftSource
{
    public static readonly DraftSource Voice = new(
        MaterialSymbols.Mic,
        sheetTitle: "Inserimento vocale",
        readingLine: "",
        privacyNote: "Audio e testo inviati a OpenAI",
        banner: heard => heard.HasValue() ? $"Dalla voce: “{heard}” · controlla e salva" : "");

    public static readonly DraftSource Photo = new(
        MaterialSymbols.PhotoCamera,
        sheetTitle: "Lettura della foto",
        readingLine: "Cerco importo, data e negozio…",
        privacyNote: "Foto inviata a OpenAI, Centesimo non la salva",
        banner: _ => "Dalla foto · controlla e salva");

    public static readonly DraftSource Pdf = new(
        MaterialSymbols.PictureAsPdf,
        sheetTitle: "Lettura del PDF",
        readingLine: "Cerco importo, data e negozio…",
        privacyNote: "PDF inviato a OpenAI, Centesimo non lo salva",
        banner: _ => "Dal PDF · controlla e salva");

    private readonly Func<string, string> _banner;

    private DraftSource(string glyph, string sheetTitle, string readingLine, string privacyNote, Func<string, string> banner)
    {
        Glyph = glyph;
        SheetTitle = sheetTitle;
        ReadingLine = readingLine;
        PrivacyNote = privacyNote;
        _banner = banner;
    }

    public string Glyph { get; }
    public string SheetTitle { get; }
    public string ReadingLine { get; }
    public string PrivacyNote { get; }

    public string Banner(string heard) => _banner(heard);

    public static DraftSource Of(ExpenseDocument document) => document.IsPdf ? Pdf : Photo;
}
