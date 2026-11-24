using Centello.Application;

namespace Centello.Infrastructure.Tests;

public sealed class ExpenseInterpretationRequestBuilder_should_expected_behavior
{
    private static readonly DateOnly Today = new(2026, 11, 23);
    private static readonly ExpenseInterpretationCatalog Catalog =
        new([new(Guid.NewGuid(), "Cibo", [new(Guid.NewGuid(), "Pizza")])]);

    [Fact]
    public void Give_the_model_local_date_and_catalog_for_italian_dictation()
    {
        var prompt = ExpenseInterpretationRequestBuilder.Voice(Catalog, Today);

        Assert.Contains("lunedì 2026-11-23", prompt);
        Assert.Contains("c1: Cibo\n  t1: Pizza", prompt);
        Assert.Contains("trascrivi solo ciò che senti", prompt);
        Assert.Contains("ieri", prompt);
    }

    [Theory]
    [InlineData("Inserisci 150 eur su lavoro perché ho acquistato la licenza di fatture in cloud", "Licenza Fatture in cloud")]
    [InlineData("Inserisci 150 eur su lavoro perché ho pagato fattura in cloud", "Pagamento fattura in cloud")]
    public void Show_how_to_turn_a_dictated_expense_reason_into_a_short_note(string dictation, string note)
    {
        var prompt = ExpenseInterpretationRequestBuilder.Voice(Catalog, Today);

        Assert.Contains($"\"{dictation}\" → note: \"{note}\"", prompt);
    }

    [Fact]
    public void Ask_for_the_amount_paid_instead_of_vat_or_change()
    {
        var prompt = ExpenseInterpretationRequestBuilder.Document(Catalog, Today);

        Assert.Contains("TOTALE EURO o IMPORTO PAGATO", prompt);
        Assert.Contains("Non usare subtotali, righe IVA, sconti, contanti versati o resto.", prompt);
        Assert.Contains("GG/MM/AAAA", prompt);
        Assert.Contains("c1: Cibo\n  t1: Pizza", prompt);
        Assert.Contains("lunedì 2026-11-23", prompt);
    }
}
