namespace Centello.Application;

/// <summary>Which expenses of a category to show by tag: every one, only those without a tag, or one tag.</summary>
public abstract record TagFilter
{
    private TagFilter()
    {
    }

    public static TagFilter All { get; } = new AllTags();
    public static TagFilter Untagged { get; } = new UntaggedOnly();
    public static TagFilter Tag(Guid tagId) => new SingleTag(tagId);

    public abstract bool Matches(Guid? expenseTagId);

    public sealed record AllTags : TagFilter
    {
        public override bool Matches(Guid? expenseTagId) => true;
    }

    public sealed record UntaggedOnly : TagFilter
    {
        public override bool Matches(Guid? expenseTagId) => !expenseTagId.HasValue;
    }

    public sealed record SingleTag(Guid TagId) : TagFilter
    {
        public override bool Matches(Guid? expenseTagId) => expenseTagId == TagId;
    }
}
