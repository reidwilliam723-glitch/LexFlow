using System.Security.Cryptography;
using System.Text.Json;

namespace Lexon.Core;

/// <summary>
/// Check-and-notify update rules. Never installs or replaces a running binary.
/// </summary>
public static class UpdatePolicy
{
    public static readonly JsonSerializerOptions ManifestJson = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public static bool ShouldCheckNow(DateTime lastCheckUtc, DateTime nowUtc, TimeSpan interval)
        => nowUtc - lastCheckUtc >= interval;

    public static int CompareVersions(string? left, string? right)
    {
        var a = Parse(left);
        var b = Parse(right);
        for (var i = 0; i < Math.Max(a.Length, b.Length); i++)
        {
            var av = i < a.Length ? a[i] : 0;
            var bv = i < b.Length ? b[i] : 0;
            if (av < bv)
            {
                return -1;
            }

            if (av > bv)
            {
                return 1;
            }
        }

        return 0;
    }

    public static bool IsNewer(string? latest, string? current)
        => CompareVersions(latest, current) > 0;

    public static VersionInfo? ParseManifest(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<VersionInfo>(json, ManifestJson);
        }
        catch
        {
            return null;
        }
    }

    public static bool MatchesSha256(byte[] content, string? expectedHex)
    {
        if (content == null || string.IsNullOrWhiteSpace(expectedHex))
        {
            return false;
        }

        var expected = expectedHex.Replace("-", string.Empty).Trim();
        var actual = Convert.ToHexString(SHA256.HashData(content));
        return actual.Equals(expected, StringComparison.OrdinalIgnoreCase);
    }

    public static string FormatNotification(UpdateCheckResult result)
    {
        var notes = string.IsNullOrWhiteSpace(result.ReleaseNotes)
            ? "Open the download page when you are ready — Lexon will not install this by itself."
            : result.ReleaseNotes.Trim();

        var privacy = string.IsNullOrWhiteSpace(result.PrivacyNotes)
            ? string.Empty
            : " Privacy/security: " + result.PrivacyNotes.Trim();

        var verified = string.IsNullOrWhiteSpace(result.Sha256)
            ? " The publisher did not include a file hash; treat the download as unverified."
            : " A SHA-256 hash is published so you can verify the file.";

        return $"Lexon {result.LatestVersion} is available (you have {result.CurrentVersion}). {notes}{privacy}{verified}";
    }

    private static int[] Parse(string? version)
    {
        if (string.IsNullOrWhiteSpace(version))
        {
            return [0];
        }

        var parts = new List<int>();
        foreach (var piece in version.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            var digits = new string(piece.TakeWhile(char.IsDigit).ToArray());
            parts.Add(int.TryParse(digits, out var n) ? n : 0);
        }

        return parts.Count == 0 ? [0] : parts.ToArray();
    }
}
