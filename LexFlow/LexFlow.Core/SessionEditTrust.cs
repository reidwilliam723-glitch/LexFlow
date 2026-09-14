namespace LexFlow.Core;

/// <summary>
/// In-memory only: after enough consecutive accepts of the same edit kind,
/// later edits of that kind skip the full preview for this process lifetime.
/// </summary>
public sealed class SessionEditTrust
{
    public const int ConsecutiveAcceptsToTrust = 3;
    private readonly Dictionary<string, int> _streaks = new(StringComparer.OrdinalIgnoreCase);

    public bool IsTrusted(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return _streaks.TryGetValue(key, out var streak) && streak >= ConsecutiveAcceptsToTrust;
    }

    public void RecordAccepted(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _streaks.TryGetValue(key, out var streak);
        _streaks[key] = streak + 1;
    }

    public void RecordRejected(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        _streaks[key] = 0;
    }
}
