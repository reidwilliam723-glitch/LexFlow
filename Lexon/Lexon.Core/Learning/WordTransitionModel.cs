namespace Lexon.Core.Learning;

/// <summary>
/// Tracks which words commonly follow other words (bigrams), combining a
/// small built-in English prior with pairs learned from the user's writing.
/// </summary>
public sealed class WordTransitionModel
{
    private const int MinWordLength = 2;
    private const int MaxPreviousWords = 8000;
    private const int MaxFollowersPerWord = 40;

    private readonly Dictionary<string, Dictionary<string, int>> _followers = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    public WordTransitionModel()
    {
        SeedCommonEnglish();
    }

    public void LearnFromCompletedText(string? userText)
    {
        var words = ExtractWords(userText);
        if (words.Count < 2)
        {
            return;
        }

        lock (_lock)
        {
            for (var i = 1; i < words.Count; i++)
            {
                ObserveUnlocked(words[i - 1], words[i], 1);
            }
        }
    }

    public void Observe(string? previousWord, string? nextWord, int weight = 1)
    {
        if (weight <= 0)
        {
            return;
        }

        lock (_lock)
        {
            ObserveUnlocked(previousWord, nextWord, weight);
        }
    }

    public double GetFollowScore(string? previousWord, string? candidate)
    {
        var previous = Normalize(previousWord);
        var next = Normalize(candidate);
        if (previous.Length == 0 || next.Length == 0)
        {
            return 0;
        }

        lock (_lock)
        {
            if (!_followers.TryGetValue(previous, out var nextCounts) || nextCounts.Count == 0)
            {
                return 0;
            }

            if (!nextCounts.TryGetValue(next, out var count))
            {
                return 0;
            }

            var max = 1;
            foreach (var value in nextCounts.Values)
            {
                if (value > max)
                {
                    max = value;
                }
            }

            return (double)count / max;
        }
    }

    public IReadOnlyList<string> GetTopFollowers(string previousWord, int count = 3)
    {
        lock (_lock)
        {
            if (string.IsNullOrWhiteSpace(previousWord) ||
                !_followers.TryGetValue(previousWord, out var followers))
            {
                return Array.Empty<string>();
            }

            return followers
                .OrderByDescending(kv => kv.Value)
                .Take(count)
                .Select(kv => kv.Key)
                .ToList();
        }
    }

    public Dictionary<string, Dictionary<string, int>> ExportCounts()
    {
        lock (_lock)
        {
            return _followers.ToDictionary(
                pair => pair.Key,
                pair => new Dictionary<string, int>(pair.Value, StringComparer.OrdinalIgnoreCase),
                StringComparer.OrdinalIgnoreCase);
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _followers.Clear();
            SeedCommonEnglish();
        }
    }

    public void ImportCounts(Dictionary<string, Dictionary<string, int>>? counts)
    {
        if (counts == null)
        {
            return;
        }

        lock (_lock)
        {
            foreach (var pair in counts)
            {
                foreach (var follower in pair.Value)
                {
                    ObserveUnlocked(pair.Key, follower.Key, Math.Max(1, follower.Value));
                }
            }
        }
    }

    private void ObserveUnlocked(string? previousWord, string? nextWord, int weight)
    {
        var previous = Normalize(previousWord);
        var next = Normalize(nextWord);
        if (previous.Length < MinWordLength || next.Length < MinWordLength)
        {
            return;
        }

        if (!_followers.TryGetValue(previous, out var nextCounts))
        {
            if (_followers.Count >= MaxPreviousWords)
            {
                return;
            }

            nextCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            _followers[previous] = nextCounts;
        }

        nextCounts.TryGetValue(next, out var current);
        nextCounts[next] = current + weight;

        if (nextCounts.Count > MaxFollowersPerWord)
        {
            var weakest = nextCounts.OrderBy(pair => pair.Value).First().Key;
            nextCounts.Remove(weakest);
        }
    }

    private static IReadOnlyList<string> ExtractWords(string? userText)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            return Array.Empty<string>();
        }

        var endsWithCompletedWord = char.IsWhiteSpace(userText[^1])
            || userText[^1] is '.' or ',' or '!' or '?' or ';' or ':';
        var tokens = userText
            .Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(token => Normalize(token))
            .Where(word => word.Length >= MinWordLength)
            .ToList();

        if (!endsWithCompletedWord && tokens.Count > 0)
        {
            tokens.RemoveAt(tokens.Count - 1);
        }

        return tokens;
    }

    private static string Normalize(string? word)
    {
        if (string.IsNullOrWhiteSpace(word))
        {
            return string.Empty;
        }

        var trimmed = word.Trim().TrimEnd('.', ',', '!', '?', ';', ':', '"', '\'', ')', ']', '}')
            .TrimStart('"', '\'', '(', '[', '{');
        if (trimmed.Length < MinWordLength || !trimmed.All(char.IsLetter))
        {
            return string.Empty;
        }

        return trimmed.ToLowerInvariant();
    }

    private void SeedCommonEnglish()
    {
        // Compact prior of high-frequency English collocations so ranking
        // works before the user has typed much. Weights are relative.
        (string Previous, string Next, int Weight)[] seed =
        {
            ("the", "same", 8), ("the", "first", 8), ("the", "most", 7), ("the", "other", 7),
            ("of", "the", 20), ("of", "a", 8), ("of", "this", 6),
            ("in", "the", 20), ("in", "a", 8), ("in", "this", 6), ("in", "order", 5),
            ("to", "the", 16), ("to", "be", 14), ("to", "do", 8), ("to", "make", 7), ("to", "get", 7), ("to", "see", 6),
            ("on", "the", 16), ("on", "a", 7), ("on", "this", 5),
            ("for", "the", 16), ("for", "a", 8), ("for", "example", 6),
            ("and", "the", 12), ("and", "then", 6),
            ("that", "the", 10), ("that", "is", 8), ("that", "was", 6),
            ("with", "the", 14), ("with", "a", 8),
            ("from", "the", 14), ("from", "a", 6),
            ("by", "the", 10), ("at", "the", 14), ("at", "least", 6),
            ("as", "the", 8), ("as", "well", 8), ("as", "a", 7),
            ("is", "the", 10), ("is", "a", 10), ("is", "not", 8),
            ("are", "the", 8), ("are", "not", 6),
            ("was", "the", 8), ("was", "a", 8), ("was", "not", 6),
            ("have", "been", 10), ("have", "to", 9), ("have", "a", 8), ("have", "the", 7),
            ("has", "been", 10), ("has", "a", 6),
            ("will", "be", 12), ("will", "not", 6), ("will", "have", 5),
            ("can", "be", 10), ("can", "not", 6),
            ("would", "be", 10), ("would", "have", 7), ("would", "like", 6), ("would", "not", 5),
            ("going", "to", 20), ("want", "to", 16), ("need", "to", 16), ("have", "to", 12),
            ("able", "to", 14), ("trying", "to", 12), ("due", "to", 10), ("according", "to", 10),
            ("out", "of", 12), ("because", "of", 10), ("instead", "of", 8), ("more", "than", 10),
            ("less", "than", 8), ("rather", "than", 6), ("such", "as", 10),
            ("look", "at", 10), ("look", "for", 8), ("look", "like", 6),
            ("based", "on", 10), ("depends", "on", 8), ("focus", "on", 6),
            ("thank", "you", 20), ("let", "me", 12), ("let", "us", 6),
            ("i", "am", 10), ("i", "have", 10), ("i", "will", 9), ("i", "would", 8), ("i", "think", 8),
            ("you", "can", 10), ("you", "are", 10), ("you", "will", 6), ("you", "have", 6),
            ("we", "are", 8), ("we", "can", 7), ("we", "will", 6), ("we", "have", 6),
            ("they", "are", 8), ("they", "have", 6), ("they", "will", 5),
            ("it", "is", 14), ("it", "was", 10), ("it", "will", 6),
            ("this", "is", 12), ("this", "was", 6),
            ("there", "is", 12), ("there", "are", 10), ("there", "was", 6),
            ("he", "was", 8), ("he", "is", 7), ("she", "was", 8), ("she", "is", 7),
            ("please", "let", 6), ("please", "send", 5)
        };

        foreach (var (previous, next, weight) in seed)
        {
            ObserveUnlocked(previous, next, weight);
        }
    }
}
