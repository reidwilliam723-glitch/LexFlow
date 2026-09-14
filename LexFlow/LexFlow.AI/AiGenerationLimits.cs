namespace LexFlow.AI;

internal static class AiGenerationLimits
{
    public const int SuggestionMaxTokens = 48;
    public static readonly TimeSpan SuggestionTimeout = TimeSpan.FromMilliseconds(2500);
    public static readonly TimeSpan RewriteTimeout = TimeSpan.FromSeconds(8);

    public static int RewriteMaxTokens(string text)
    {
        var length = string.IsNullOrEmpty(text) ? 0 : text.Length;
        return Math.Clamp(length / 4 + 48, 96, 512);
    }
}
