using System.Collections.Frozen;
using System.Text.Json;
using Centello.Application;

namespace Centello.App;

internal static class FirebaseAiSettings
{
    public static AiRemoteSettings? Parse(string? surveyUrl, string? revokedInviteIds)
    {
        if (revokedInviteIds is null)
            return null;

        try
        {
            using var document = JsonDocument.Parse(revokedInviteIds);
            if (document.RootElement.ValueKind != JsonValueKind.Array)
                return null;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var element in document.RootElement.EnumerateArray())
            {
                if (element.ValueKind != JsonValueKind.String || element.GetString() is not { Length: 32 } id ||
                    id.Any(character => character is not (>= '0' and <= '9' or >= 'a' and <= 'f')))
                    return null;

                ids.Add(id);
            }

            return new AiRemoteSettings(surveyUrl ?? "", ids.ToFrozenSet(StringComparer.Ordinal));
        }
        catch (Exception exception) when (exception is JsonException or InvalidOperationException or ArgumentException)
        {
            return null;
        }
    }
}
