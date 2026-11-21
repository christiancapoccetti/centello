namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record NewHabits(string Headline, string Detail, IReadOnlyList<RecapTagLine> Habits) : RecapCard
    {
        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.NewHabits(this);
    }
}
