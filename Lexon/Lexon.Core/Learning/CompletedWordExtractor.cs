namespace Lexon.Core.Learning;

/// <summary>
/// Pulls finished words out of typed text so in-progress prefixes
/// ("hel" while typing "hello") are not stored as vocabulary.
/// </summary>
public static class CompletedWordExtractor
{
    public static IReadOnlyList<string> Extract(string? userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            return Array.Empty<string>();
        }

        var endsWithCompletedWord = char.IsWhiteSpace(userText[^1]) || IsWordEndingPunctuation(userText[^1]);
        var tokens = userText
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(TrimPunctuation)
            .Where(w => w.Length >= 3 && w.All(char.IsLetter))
            .ToList();

        if (!endsWithCompletedWord && tokens.Count > 0)
        {
            tokens.RemoveAt(tokens.Count - 1);
        }

        return tokens;
    }

    private static bool IsWordEndingPunctuation(char c)
        => c is '.' or ',' or '!' or '?' or ';' or ':' or ')' or ']' or '"' or '\'';

    private static string TrimPunctuation(string word)
    {
        return word.Trim().TrimEnd('.', ',', '!', '?', ';', ':', '"', '\'', ')', ']', '}')
                    .TrimStart('"', '\'', '(', '[', '{');
    }
}
