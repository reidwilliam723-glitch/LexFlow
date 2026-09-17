namespace Lexon.Core;

public static class WritingIssueParser
{
    public static List<string> Parse(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw) || raw.Trim().Equals("NONE", StringComparison.OrdinalIgnoreCase))
        {
            return [];
        }

        return raw.Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.StartsWith("ISSUE:", StringComparison.OrdinalIgnoreCase) ? line[6..].Trim() : line)
            .Where(line => line.Length > 0 && !line.Equals("NONE", StringComparison.OrdinalIgnoreCase))
            .Take(8)
            .ToList();
    }
}
