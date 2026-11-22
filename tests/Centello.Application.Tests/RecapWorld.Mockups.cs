using Centello.Domain;

namespace Centello.Application.Tests;

/// <summary>The data of the two approved mockups, as expenses. The mockups contradict themselves in places (the small
/// expenses of November cannot add up to their 84,60 with 22 of them at 1,30; the six named categories cannot all
/// outweigh the three "altre"; the automatic payments of the year do not add up to their 5.566), so the worlds keep
/// every figure that can hold together and say so below.</summary>
internal sealed partial class RecapWorld
{
    private static readonly DateOnly Saturday8August = new(2026, 8, 8);

    /// <summary>"Il tuo novembre" as the mockup draws it, on 1 December 2026: 96 expenses for 1.284,60 euros; the
    /// second lightest month of the year; Saturday 14 the record day; Casa first; Ristoranti up and Trasporti down
    /// against October; Piscina the new habit; 31 small expenses; 26 days out of 30; 16.071 euros so far against
    /// 16.420. The small expenses add up to 69,10 and the categories the mockup folds are three of 58,00.</summary>
    public static RecapWorld MockupNovember()
    {
        var world = new RecapWorld(Today);
        var casa = world.Category("Casa", "peri");
        var spesa = world.Category("Spesa", "mint");
        var ristoranti = world.Category("Ristoranti", "butter");
        var trasporti = world.Category("Trasporti", "sky");
        var svago = world.Category("Svago", "lilac");
        var bar = world.Category("Bar", "coral");
        var salute = world.Category("Salute", "sky");
        var regali = world.Category("Regali", "mint");
        var abbigliamento = world.Category("Abbigliamento", "butter");
        var caffe = world.Tag(bar, "Caffè");
        var piscina = world.Tag(svago, "Piscina");

        world.MockupNovember2026(casa, spesa, ristoranti, trasporti, svago, bar, salute, regali, abbigliamento, caffe, piscina);
        world.MockupOctober2026(casa, spesa, ristoranti, trasporti, svago, bar, salute, regali, abbigliamento, caffe);
        world.MockupEarlierMonths(spesa);
        return world;
    }

    private void MockupNovember2026(Category casa, Category spesa, Category ristoranti, Category trasporti, Category svago,
        Category bar, Category salute, Category regali, Category abbigliamento, Tag caffe, Tag piscina)
    {
        // 26 days with an expense: 14 holds the record trio, and 2, 9, 23 and 30 stay empty.
        var days = Enumerable.Range(1, 30).Where(day => day is not (2 or 9 or 14 or 23 or 30)).ToArray();
        var next = 0;

        void Many(Category category, int count, long cents, Tag? tag = null)
        {
            for (var index = 0; index < count; index++)
                Spend(category, cents, new DateOnly(2026, 11, days[next++ % days.Length]), tag);
        }

        Spend(casa, 32_000, new DateOnly(2026, 11, 1), note: "Affitto", payment: Guid.NewGuid());
        Spend(spesa, 9_210, new DateOnly(2026, 11, 14), note: "Spesa della settimana");
        Spend(ristoranti, 6_400, new DateOnly(2026, 11, 14), note: "Cena fuori");
        Spend(trasporti, 3_030, new DateOnly(2026, 11, 14), note: "Benzina");
        Many(spesa, 5, 450);
        Many(spesa, 10, 1_978);
        Many(ristoranti, 5, 2_370);
        Many(trasporti, 4, 450);
        Many(trasporti, 10, 929);
        foreach (var day in new[] { 4, 11, 18, 22, 25 })
            Spend(svago, 850, new DateOnly(2026, 11, day), piscina);

        Many(svago, 10, 538);
        Many(bar, 22, 130, caffe);
        Many(bar, 4, 740);
        Many(salute, 8, 725);
        Many(regali, 5, 1_160);
        Many(abbigliamento, 4, 1_450);
    }

    private void MockupOctober2026(Category casa, Category spesa, Category ristoranti, Category trasporti, Category svago,
        Category bar, Category salute, Category regali, Category abbigliamento, Tag caffe)
    {
        Spend(casa, 32_000, new DateOnly(2026, 10, 1), note: "Affitto", payment: Guid.NewGuid());
        SpendInMonth(spesa, 2026, 10, 31_240, 4);
        SpendInMonth(ristoranti, 2026, 10, 11_830, 3);
        SpendInMonth(trasporti, 2026, 10, 18_980, 3);
        SpendInMonth(svago, 2026, 10, 9_630, 3);
        Spend(bar, 130, new DateOnly(2026, 10, 5), caffe);
        Spend(bar, 130, new DateOnly(2026, 10, 12), caffe);
        Spend(bar, 5_560, new DateOnly(2026, 10, 6));
        foreach (var category in new[] { salute, regali, abbigliamento })
            SpendInMonth(category, 2026, 10, 10_060, 3);
    }

    private void MockupEarlierMonths(Category spesa)
    {
        long[] earlier2026 = [151_210, 119_800, 142_000, 135_500, 147_600, 139_000, 170_200, 194_800, 138_800];
        long[] months2025 =
            [147_940, 130_200, 139_500, 141_000, 152_000, 145_500, 181_000, 201_000, 140_200, 140_000, 123_610];
        for (var index = 0; index < earlier2026.Length; index++)
            Spend(spesa, earlier2026[index], new DateOnly(2026, index + 1, 10));

        for (var index = 0; index < months2025.Length; index++)
            Spend(spesa, months2025[index], new DateOnly(2025, index + 1, 10));
    }

    /// <summary>"Il tuo 2026" on 5 January 2027: 17.955 euros, dearest month August and lightest February, the record
    /// day Saturday 8 August, Saturday 24% of the variable expenses, Caffè the tag of the year, Pizza the costliest,
    /// Pane the steadiest, three new habits, the small expenses, the automatic payments and the year heatmap. The
    /// year before cost 18.340. The mockup makes Caffè both the tag of the year and the steadiest and the automatic
    /// payments total 4.872, not 5.566, so those two differ from it.</summary>
    public static RecapWorld Mockup2026()
    {
        var world = new RecapWorld(new DateOnly(2027, 1, 5));
        var cast = new YearCast(
            world.Category("Casa", "peri"), world.Category("Spesa", "mint"), world.Category("Ristoranti", "butter"),
            world.Category("Trasporti", "sky"), world.Category("Svago", "lilac"), world.Category("Bar", "coral"));
        var tags = new YearTags(
            world.Tag(cast.Bar, "Caffè"), world.Tag(cast.Ristoranti, "Pizza"), world.Tag(cast.Svago, "Piscina"),
            world.Tag(cast.Svago, "Libri"), world.Tag(cast.Spesa, "Veterinario"), world.Tag(cast.Spesa, "Pane"));
        world.Mockup2026Year(cast, tags);
        world.Mockup2025Year(cast, tags);
        return world;
    }

    private sealed record YearCast(Category Casa, Category Spesa, Category Ristoranti, Category Trasporti, Category Svago,
        Category Bar);

    private sealed record YearTags(Tag Caffe, Tag Pizza, Tag Piscina, Tag Libri, Tag Veterinario, Tag Pane);

    /// <summary>The spending of a year month by month, so that what is left to a month's total can be filled.</summary>
    private sealed class YearPlan(RecapWorld world, int year)
    {
        public long[] Spent { get; } = new long[12];
        public int Variable { get; private set; }
        public int VariableOnSaturday { get; private set; }

        public void Spend(Category category, long cents, DateOnly on, Tag? tag = null, string note = "", Guid? payment = null)
        {
            world.Spend(category, cents, on, tag, note, payment: payment);
            Spent[on.Month - 1] += cents;
            if (payment.HasValue || on.Year != year)
                return;

            Variable++;
            if (on.DayOfWeek == DayOfWeek.Saturday)
                VariableOnSaturday++;
        }

        public void FillMonths(Category category, long[] targets, Func<int, DateOnly> dayOf, Func<int, int> piecesIn)
        {
            for (var month = 1; month <= 12; month++)
            {
                var pieces = piecesIn(month);
                var left = targets[month - 1] - Spent[month - 1];
                for (var piece = 0; piece < pieces; piece++)
                    Spend(category, left / pieces + (piece == 0 ? left % pieces : 0), dayOf(piece + 12 * month));
            }
        }
    }

    private static readonly long[] Months2026 =
        [151_200, 119_800, 142_000, 135_500, 147_600, 139_000, 170_200, 194_800, 138_800, 139_700, 128_500, 188_400];

    private static readonly long[] Months2025 =
        [152_800, 152_800, 152_800, 152_800, 152_800, 152_800, 152_800, 152_800, 152_800, 152_800, 152_800, 153_200];

    private static readonly (string Name, long Cents, Func<YearCast, Category> Category)[] Payments =
    [
        ("Affitto", 32_000, cast => cast.Casa), ("Assicurazione auto", 5_100, cast => cast.Trasporti),
        ("Netflix", 1_399, cast => cast.Svago),
        ("Spotify", 1_099, cast => cast.Svago), ("Telefono", 999, cast => cast.Casa)
    ];

    private void PayEveryMonth(YearPlan plan, YearCast cast, int year)
    {
        foreach (var (name, cents, categoryOf) in Payments)
        {
            var payment = Guid.NewGuid();
            for (var month = 1; month <= 12; month++)
                plan.Spend(categoryOf(cast), cents, new DateOnly(year, month, 1), note: name, payment: payment);
        }
    }

    private void Mockup2026Year(YearCast cast, YearTags tags)
    {
        var plan = new YearPlan(this, 2026);
        PayEveryMonth(plan, cast, 2026);
        var newYear = new DateOnly(2026, 1, 1);

        for (var use = 0; use < 262; use++)
        {
            var day = newYear.AddDays(use * 365 / 262);
            plan.Spend(cast.Bar, 130, day == Saturday8August ? day.AddDays(1) : day, tags.Caffe);
        }

        foreach (var week in Enumerable.Range(1, 52).Except([2, 5, 9, 13, 16, 20, 24, 28, 31, 35, 40, 44]))
            plan.Spend(cast.Spesa, 280, newYear.AddDays(7 * (week - 1) + 3), tags.Pane);

        SpendTag(plan, cast.Ristoranti, tags.Pizza, 1_250, 2026, [3, 4, 2, 3, 5, 4, 3, 2, 4, 3, 4, 4], firstDay: 4, step: 6, lastCents: 1_240);
        SpendTag(plan, cast.Svago, tags.Piscina, 850, 2026, [0, 0, 0, 0, 0, 0, 0, 0, 5, 5, 4, 4], firstDay: 2, step: 6);
        SpendTag(plan, cast.Svago, tags.Libri, 1_630, 2026, [0, 0, 3, 3, 3], firstDay: 5, step: 7);
        SpendTag(plan, cast.Spesa, tags.Veterinario, 5_250, 2026, [0, 0, 0, 0, 0, 1, 1, 1, 1], firstDay: 15, step: 1);
        plan.Spend(cast.Trasporti, 28_600, Saturday8August, note: "Traghetto per l'Elba");
        plan.Spend(cast.Ristoranti, 8_450, Saturday8August, note: "Cena al porto");
        plan.Spend(cast.Spesa, 4_230, Saturday8August, note: "Spesa per la casa al mare");

        for (var month = 1; month <= 12; month++)
        {
            plan.Spend(cast.Ristoranti, month <= 6 ? 13_193 : 13_192, new DateOnly(2026, month, 17));
            plan.Spend(cast.Trasporti, 8_850, new DateOnly(2026, month, 18));
            plan.Spend(cast.Svago, 5_000, new DateOnly(2026, month, 19));
            plan.Spend(cast.Bar, 2_000, new DateOnly(2026, month, 21));
        }

        // Saturday carries about a quarter of the variable expenses: the Saturdays fill what is left of each month.
        var ballast = (int)Math.Round((0.24 * plan.Variable - plan.VariableOnSaturday) / 0.76);
        plan.FillMonths(cast.Spesa, Months2026, key => SaturdayOf(2026, key / 12, key % 12), month => ballast / 12 + (month <= ballast % 12 ? 1 : 0));
    }

    private void Mockup2025Year(YearCast cast, YearTags tags)
    {
        var plan = new YearPlan(this, 2025);
        PayEveryMonth(plan, cast, 2025);
        plan.Spend(cast.Bar, 130, new DateOnly(2025, 1, 15), tags.Caffe);
        plan.Spend(cast.Ristoranti, 1_250, new DateOnly(2025, 1, 16), tags.Pizza);
        plan.Spend(cast.Spesa, 280, new DateOnly(2025, 1, 17), tags.Pane);
        for (var month = 1; month <= 12; month++)
        {
            plan.Spend(cast.Ristoranti, month == 1 ? 11_750 : 13_000, new DateOnly(2025, month, 17));
            plan.Spend(cast.Trasporti, month <= 4 ? 15_734 : 15_733, new DateOnly(2025, month, 18));
            plan.Spend(cast.Svago, month <= 6 ? 7_498 : 7_497, new DateOnly(2025, month, 19));
            plan.Spend(cast.Bar, month <= 4 ? 4_839 : 4_838, new DateOnly(2025, month, 21));
        }

        plan.FillMonths(cast.Spesa, Months2025, key => new DateOnly(2025, key / 12, key % 4 + 4), _ => 4);
    }

    private static void SpendTag(YearPlan plan, Category category, Tag tag, long cents, int year, int[] perMonth, int firstDay,
        int step, long? lastCents = null)
    {
        var uses = 0;
        var total = perMonth.Sum();
        for (var month = 1; month <= perMonth.Length; month++)
            for (var use = 0; use < perMonth[month - 1]; use++)
                plan.Spend(category, ++uses == total ? lastCents ?? cents : cents,
                    new DateOnly(year, month, firstDay + use * step), tag);
    }

    /// <summary>The Saturday of <paramref name="month"/> that piece <paramref name="piece"/> falls on, never the record day.</summary>
    private static DateOnly SaturdayOf(int year, int month, int piece)
    {
        var saturdays = Enumerable.Range(1, DateTime.DaysInMonth(year, month))
            .Select(day => new DateOnly(year, month, day))
            .Where(day => day.DayOfWeek == DayOfWeek.Saturday && day != Saturday8August)
            .ToList();
        return saturdays[piece % saturdays.Count];
    }
}
