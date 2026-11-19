using System.Buffers.Binary;
using System.Text;

namespace Centesimo.Application.Tests;

/// <summary>Reads just enough of a TrueType file (table directory and cmap) to check what a bundled font draws.</summary>
internal static class FontFiles
{
    public const string IconFontRelativePath = "src/Centesimo.App/Resources/Fonts/MaterialSymbolsRounded-Filled.ttf";

    public static byte[] ReadIconFont() => File.ReadAllBytes(Path.Combine(RepoRoot(), IconFontRelativePath));

    public static uint SfntVersion(byte[] font) => U32(font, 0);

    public static IReadOnlySet<string> Tables(byte[] font) => Enumerable
        .Range(0, U16(font, 4))
        .Select(index => Encoding.ASCII.GetString(font, 12 + index * 16, 4))
        .ToHashSet();

    public static IReadOnlySet<int> MappedCodepoints(byte[] font)
    {
        var cmap = TableOffset(font, "cmap");
        var mapped = new HashSet<int>();
        for (var index = 0; index < U16(font, cmap + 2); index++)
        {
            var record = cmap + 4 + index * 8;
            var platform = U16(font, record);
            if (platform is not (0 or 3))
                continue;

            var subtable = cmap + (int)U32(font, record + 4);
            switch (U16(font, subtable))
            {
                case 4:
                    AddSegmentedRanges(font, subtable, mapped);
                    break;
                case 12:
                    AddSequentialGroups(font, subtable, mapped);
                    break;
            }
        }

        return mapped;
    }

    private static void AddSegmentedRanges(byte[] font, int subtable, HashSet<int> mapped)
    {
        var segments = U16(font, subtable + 6) / 2;
        var endCodes = subtable + 14;
        var startCodes = endCodes + segments * 2 + 2;
        var deltas = startCodes + segments * 2;
        var rangeOffsets = deltas + segments * 2;
        for (var segment = 0; segment < segments; segment++)
        {
            var start = U16(font, startCodes + segment * 2);
            var end = U16(font, endCodes + segment * 2);
            var delta = U16(font, deltas + segment * 2);
            var rangeOffsetPosition = rangeOffsets + segment * 2;
            var rangeOffset = U16(font, rangeOffsetPosition);
            for (var code = start; code <= end && code != 0xFFFF; code++)
            {
                var glyph = rangeOffset == 0
                    ? (code + delta) & 0xFFFF
                    : IndexedGlyph(font, rangeOffsetPosition + rangeOffset + 2 * (code - start), delta);
                if (glyph != 0)
                    mapped.Add(code);
            }
        }
    }

    private static int IndexedGlyph(byte[] font, int position, int delta)
    {
        var glyph = U16(font, position);
        return glyph == 0 ? 0 : (glyph + delta) & 0xFFFF;
    }

    private static void AddSequentialGroups(byte[] font, int subtable, HashSet<int> mapped)
    {
        var groups = (int)U32(font, subtable + 12);
        for (var group = 0; group < groups; group++)
        {
            var record = subtable + 16 + group * 12;
            var start = (int)U32(font, record);
            var end = (int)U32(font, record + 4);
            var firstGlyph = U32(font, record + 8);
            for (var code = start; code <= end; code++)
                if (firstGlyph + (uint)(code - start) != 0)
                    mapped.Add(code);
        }
    }

    private static int TableOffset(byte[] font, string tag)
    {
        for (var index = 0; index < U16(font, 4); index++)
        {
            var record = 12 + index * 16;
            if (Encoding.ASCII.GetString(font, record, 4) == tag)
                return (int)U32(font, record + 8);
        }

        throw new InvalidOperationException($"The font has no '{tag}' table.");
    }

    private static string RepoRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Centesimo.slnx")))
            directory = directory.Parent;

        return directory?.FullName ?? throw new InvalidOperationException("Centesimo.slnx was not found above the test output.");
    }

    private static int U16(byte[] font, int offset) => BinaryPrimitives.ReadUInt16BigEndian(font.AsSpan(offset));

    private static uint U32(byte[] font, int offset) => BinaryPrimitives.ReadUInt32BigEndian(font.AsSpan(offset));
}
