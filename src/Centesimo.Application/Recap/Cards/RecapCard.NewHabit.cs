namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record NewHabit(RecapCategory Category, string Lead, string Tag, RecapPhrase Detail, RecapBars Days,
        string From, string To) : RecapCard
    {
        public override RecapSurface Surface => RecapSurface.ForCategory(Category.ColorKey);

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.NewHabit(this);
    }
}
