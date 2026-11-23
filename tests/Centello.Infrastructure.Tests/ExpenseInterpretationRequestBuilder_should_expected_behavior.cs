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
