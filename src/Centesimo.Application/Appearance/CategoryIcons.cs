namespace Centesimo.Application;

public sealed record CategoryIcon(string Key, string Label, string Glyph);

public static class CategoryIcons
{
    public static readonly CategoryIcon Fallback = new("more", "Altro", MaterialSymbols.MoreHoriz);

    /// <summary>Picker order. Keys are persisted in Categories.Icon (max 40): never rename or remove one.</summary>
    public static IReadOnlyList<CategoryIcon> All { get; } =
    [
        new("cart", "Carrello", MaterialSymbols.ShoppingCart),
        new("home", "Casa", MaterialSymbols.Home),
        new("car", "Auto", MaterialSymbols.DirectionsCar),
        new("heart", "Cuore", MaterialSymbols.Favorite),
        new("sport", "Sport", MaterialSymbols.SportsSoccer),
        new("shopping-bag", "Shopping", MaterialSymbols.ShoppingBag),
        new("food", "Cibo", MaterialSymbols.Restaurant),
        new("gift", "Regalo", MaterialSymbols.Redeem),
        new("beach", "Mare", MaterialSymbols.BeachAccess),
        new("tech", "Tecnologia", MaterialSymbols.Devices),
        new("school", "Scuola", MaterialSymbols.School),
        new("family", "Famiglia", MaterialSymbols.FamilyRestroom),
        new("gym", "Palestra", MaterialSymbols.FitnessCenter),
        new("gear", "Manutenzione", MaterialSymbols.Handyman),
        new("coffee", "Caffè", MaterialSymbols.LocalCafe),
        new("health", "Salute", MaterialSymbols.Medication),
        new("bills", "Bollette", MaterialSymbols.Bolt),
        new("bus", "Mezzi pubblici", MaterialSymbols.DirectionsBus),
        new("travel", "Viaggi", MaterialSymbols.Flight),
        new("pets", "Animali", MaterialSymbols.Pets),
        new("subscriptions", "Abbonamenti", MaterialSymbols.Subscriptions),
        new("leisure", "Svago", MaterialSymbols.ConfirmationNumber),
        new("clothing", "Abbigliamento", MaterialSymbols.Checkroom),
        new("work", "Lavoro", MaterialSymbols.Work),
        new("fuel", "Carburante", MaterialSymbols.LocalGasStation),
        new("phone", "Telefono", MaterialSymbols.Smartphone),
        new("personal-care", "Cura personale", MaterialSymbols.ContentCut),
        new("kids", "Bambini", MaterialSymbols.ChildFriendly),
        new("taxes", "Tasse", MaterialSymbols.ReceiptLong),
        new("books", "Libri", MaterialSymbols.MenuBook),
        new("smoking", "Sigarette", MaterialSymbols.SmokingRooms),
        Fallback
    ];

    private static readonly IReadOnlyDictionary<string, CategoryIcon> ByKey =
        All.ToDictionary(icon => icon.Key, StringComparer.Ordinal);

    /// <summary>The icon stored under <paramref name="key"/>, or <see cref="Fallback"/> for a missing or unknown key.</summary>
    public static CategoryIcon Resolve(string? key) =>
        key is not null && ByKey.TryGetValue(key, out var icon) ? icon : Fallback;
}
