namespace Lexon.Core;

public static class EditRisk
{
    public const int LowRiskWordLimit = 18;

    public static bool IsLowRisk(string? before, string? after)
    {
        return CountWords(before) <= LowRiskWordLimit && CountWords(after) <= LowRiskWordLimit + 6;
    }

    public static int CountWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return 0;
        }

        return text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
    }
}
