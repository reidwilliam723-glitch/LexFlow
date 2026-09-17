using Lexon.Core.Models;

namespace Lexon.Core.History;

/// <summary>
/// Manages suggestion history for undo functionality
/// </summary>
public class SuggestionHistory
{
    private readonly Stack<SuggestionHistoryEntry> _history = new();
    private const int MaxHistorySize = 50;

    public int Count => _history.Count;
    public bool CanUndo => _history.Count > 0;

    public void AddEntry(Suggestion suggestion, string originalText, string applicationName, bool accepted = false)
    {
        var entry = new SuggestionHistoryEntry
        {
            Suggestion = suggestion,
            OriginalText = originalText,
            Timestamp = DateTime.UtcNow,
            ApplicationName = applicationName,
            Accepted = accepted
        };

        _history.Push(entry);

        // Trim history if it gets too large
        while (_history.Count > MaxHistorySize)
        {
            _history.Pop();
        }
    }

    public SuggestionHistoryEntry? Undo()
    {
        return _history.Count > 0 ? _history.Pop() : null;
    }

    public SuggestionHistoryEntry? Peek()
    {
        return _history.Count > 0 ? _history.Peek() : null;
    }

    public void Clear()
    {
        _history.Clear();
    }

    public IEnumerable<SuggestionHistoryEntry> GetRecentEntries(int count = 10)
    {
        return _history.Take(count);
    }
}

public class SuggestionHistoryEntry
{
    public Suggestion Suggestion { get; set; } = null!;
    public string OriginalText { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public bool Accepted { get; set; }
}
