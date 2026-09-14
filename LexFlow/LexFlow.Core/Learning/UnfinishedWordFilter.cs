namespace LexFlow.Core.Learning;

/// <summary>
/// Drops tokens that look like abandoned prefixes of something the overlay
/// was already offering (typed "hel", saw "hello", hit Enter or space).
/// </summary>
public static class UnfinishedWordFilter
{
    public static IReadOnlyList<string> Apply(
        IEnumerable<string> words,
        IReadOnlyList<string>? offeredCompletions)
    {
        var list = words?.ToList() ?? new List<string>();
        if (list.Count == 0 || offeredCompletions == null || offeredCompletions.Count == 0)
        {
            return list;
        }

        return list
            .Where(word => !IsPrefixOfOfferedCompletion(word, offeredCompletions))
            .ToList();
    }

    public static bool IsPrefixOfOfferedCompletion(string word, IReadOnlyList<string> offeredCompletions)
    {
        if (string.IsNullOrEmpty(word))
        {
            return false;
        }

        foreach (var offered in offeredCompletions)
        {
            if (string.IsNullOrEmpty(offered) || offered.Length <= word.Length)
            {
                continue;
            }

            if (offered.StartsWith(word, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
