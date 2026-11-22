namespace Centello.Application;

public abstract partial record RecapCard
{
    /// <summary>Where a month falls among the months of its year so far, only when it is among the first three from
    /// either end. Money card: every expense counts.</summary>
    public sealed record Rank(string Headline, string Detail, RecapBars Chart) : RecapCard
    {
        private sealed record Side(string Superlative, string Than);

        private static readonly Side Lightest = new("più leggero", "meno");
        private static readonly Side Dearest = new("più caro", "di più");

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Rank(this);

        internal static RecapCard? Of(RecapScope scope)
        {
            if (scope.YearToDate is not { } yearToDate)
                return null;

            var period = scope.Span.Period;
            var totals = yearToDate.Current
                .GroupBy(expense => expense.OccurredOn.Month)
                .ToDictionary(group => group.Key, group => group.Sum(expense => expense.Amount.Cents));
            if (totals.Count < RecapRules.MinRankMonths || !totals.TryGetValue(period.Start.Month, out var mine))
                return null;

            var lighter = totals.Where(other => other.Value < mine).OrderBy(other => other.Value).ThenBy(other => other.Key).ToList();
            var heavier = totals.Where(other => other.Value > mine).OrderByDescending(other => other.Value).ThenBy(other => other.Key).ToList();
            var isLightest = lighter.Count <= heavier.Count;
            var ahead = isLightest ? lighter : heavier;
            var behind = isLightest ? heavier : lighter;
            var rank = ahead.Count + 1;
            if (rank > RecapRules.RankReach || behind.Count == 0)
                return null;

            var side = isLightest ? Lightest : Dearest;
            var year = period.Start.Year;
            var month = ItalianFormat.MonthName(period.Start.Month);
            var ordinal = RecapTexts.Ordinal(rank);
            var cut = scope.YearToDateIsCut ? $" fino a {month}" : "";
            return new Rank($"Il {ordinal}mese {side.Superlative} del {year}{cut}",
                DetailOf(rank, side, isLightest ? Dearest : Lightest, ahead, behind[0]),
                new RecapBars(Bars(period.Start.Month, totals, ahead.Select(other => other.Key).ToHashSet()), [],
                    $"Spesa per mese nel {year}: {month} {ItalianFormat.WholeMoney(mine)}, il {ordinal}{side.Superlative}"))
            {
                Row = new RecapSummaryRow(MaterialSymbols.CalendarMonth, scope.YearToDateIsCut ? $"Fino a {month}" : "Nell'anno",
                    rank == 1 ? RecapTexts.Capitalized(side.Superlative) : $"{rank}° {side.Superlative}")
            };
        }

        private static string DetailOf(int rank, Side side, Side opposite, List<KeyValuePair<int, long>> ahead,
            KeyValuePair<int, long> extreme) => rank switch
        {
            1 => $"Il {opposite.Superlative} è stato {ItalianFormat.MonthName(extreme.Key)}: {ItalianFormat.WholeMoney(extreme.Value)}.",
            2 => $"Solo {ItalianFormat.MonthName(ahead[0].Key)} è costato {side.Than}: {ItalianFormat.WholeMoney(ahead[0].Value)}.",
            _ => $"Solo {RecapTexts.List([.. ahead.Select(other => ItalianFormat.MonthName(other.Key))])} sono costati {side.Than}."
        };

        private static List<RecapBar> Bars(int month, Dictionary<int, long> totals, HashSet<int> cited)
        {
            var peak = totals.Where(total => total.Key <= month).Max(total => total.Value);
            return Enumerable.Range(1, month)
                .Select(index => Bar(index, totals.GetValueOrDefault(index), peak, index == month, cited.Contains(index)))
                .ToList();
        }

        private static RecapBar Bar(int month, long cents, long peak, bool isCurrent, bool isCited)
        {
            var label = ItalianFormat.MonthInitial(month);
            if (cents == 0)
                return new RecapBar(0, RecapBarKind.Stub, Label: label);

            var height = (double)cents / peak;
            if (isCurrent)
                return new RecapBar(height, RecapBarKind.Strong, ItalianFormat.WholeEuros(cents), label);

            return isCited
                ? new RecapBar(height, RecapBarKind.Bar, ItalianFormat.WholeEuros(cents), label)
                : new RecapBar(height, RecapBarKind.Bar, Label: label);
        }
    }
}
