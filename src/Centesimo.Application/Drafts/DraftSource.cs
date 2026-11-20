using Centesimo.Domain;

namespace Centesimo.Application;

/// <summary>
/// Where a draft comes from. Each source carries the glyph and the words the home sheet and the editor banner show
/// for it, so a new source is one more instance here and no screen branches on the kind.
/// </summary>
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
        sheetTitle: "Lettura scontrino",
        readingLine: "Cerco importo, data e negozio…",
        privacyNote: "Foto inviata a OpenAI, Centesimo non la salva",
        banner: _ => "Dalla foto · controlla e salva");

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
    /// <summary>What the sheet says under the progress bar while OpenAI reads. Empty when the title says enough.</summary>
    public string ReadingLine { get; }
    public string PrivacyNote { get; }

    /// <param name="heard">What the voice heard. A photo has nothing to quote.</param>
    /// <returns>Empty when the source has nothing to say about the draft.</returns>
    public string Banner(string heard) => _banner(heard);
}
