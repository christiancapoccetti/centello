namespace Centello.App;

internal static class ExportCache
{
    public static string NewShareDirectory()
    {
        var root = Path.Combine(FileSystem.CacheDirectory, "sharing-root", "centello-exports");
        var cutoff = DateTime.UtcNow.AddDays(-1);
        try
        {
            foreach (var directory in Directory.EnumerateDirectories(root))
            {
                if (Directory.GetLastWriteTimeUtc(directory) < cutoff)
                    Delete(directory);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }

        // Android opens the chooser before the receiving app reads the URI. Keep the shared copy until it expires.
        return Path.Combine(root, Guid.NewGuid().ToString("N"));
    }

    public static void Delete(string directory)
    {
        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
        }
    }
}
