using System.Text;

namespace Centello.App.Controls.Recap;

internal sealed partial class SpendingMapArt
{
    public string FocusHeadline =>
        $"«{Name(_map.FocusTag.Name, "Tag")}» · {Spoken(_map.Share(_map.FocusTag.TotalCents))} delle spese";

    public string FocusCaption =>
        $"{Name(_map.FocusCategory.Category.Name, "Categoria")} → {Name(_map.FocusTag.Name, "Tag")} · {Value(_map.FocusTag.TotalCents)} del totale";

    public string Describe()
    {
        var scope = _complete ? "Mappa completa" : "Storia";
        var units = _amounts ? $"Spese totali, {Value(_map.TotalCents)}" : "Percentuali sulle spese totali";
        var text = new StringBuilder($"{scope}. {_map.PeriodLabel}. {units}. ");
        var focus = _map.FocusTag;
        text.Append($"{FocusHeadline}. Categoria {Name(_map.FocusCategory.Category.Name, "Categoria")}. ");
        text.Append($"Automatiche con questo tag, {Spoken(Value(focus.AutomaticCents))}. Variabili, {Spoken(Value(focus.VariableCents))}.");
        if (!_complete)
        {
            if (_amounts)
                text.Append($" Totale del tag, {Value(focus.TotalCents)}.");
            return text.ToString();
        }

        foreach (var category in _map.Categories)
        {
            text.Append($" {Name(category.Category.Name, "Categoria")}, {Spoken(Value(category.TotalCents))}.");
            foreach (var tag in category.Tags)
                text.Append($" {Name(tag.Name, "Tag")}, {Spoken(Value(tag.TotalCents))}.");
        }
        return text.ToString();
    }

    private static string Spoken(string value) => value == "<1%" ? "meno dell’1%" : value;
}
