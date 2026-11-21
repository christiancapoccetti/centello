namespace Centesimo.Application;

public abstract partial record RecapCard
{
    public sealed record Changes(RecapChange? Rise, RecapChange? Fall) : RecapCard
    {
        public override RecapSurface Top => (Rise ?? Fall)!.Surface;
        public override RecapSurface Bottom => (Fall ?? Rise)!.Surface;

        public override T Accept<T>(IRecapCardVisitor<T> visitor) => visitor.Changes(this);
    }
}
