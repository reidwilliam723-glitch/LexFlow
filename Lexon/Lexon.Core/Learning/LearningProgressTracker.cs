namespace Lexon.Core.Learning;

/// <summary>
/// Tracks learning progress and statistics
/// </summary>
public class LearningProgressTracker
{
    private LearningStatistics _statistics = new();
    private readonly Dictionary<string, int> _wordFrequency = new();
    private readonly Dictionary<string, int> _suggestionAcceptance = new();
    private DateTime _sessionStartTime = DateTime.UtcNow;

    public LearningStatistics Statistics => _statistics;

    public void RecordSuggestionShown(string suggestion, string source)
    {
        _statistics.TotalSuggestionsShown++;
        
        switch (source.ToLowerInvariant())
        {
            case "dictionary":
                _statistics.DictionarySuggestionsShown++;
                break;
            case "learned":
                _statistics.LearnedSuggestionsShown++;
                break;
            case "ai":
                _statistics.AISuggestionsShown++;
                break;
        }
    }

    public void RecordSuggestionAccepted(string suggestion, string source)
    {
        _statistics.TotalSuggestionsAccepted++;
        
        switch (source.ToLowerInvariant())
        {
            case "dictionary":
                _statistics.DictionarySuggestionsAccepted++;
                break;
            case "learned":
                _statistics.LearnedSuggestionsAccepted++;
                break;
            case "ai":
                _statistics.AISuggestionsAccepted++;
                break;
        }

        // Track word frequency for learning
        var words = suggestion.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var word in words)
        {
            var normalizedWord = word.ToLowerInvariant();
            _wordFrequency[normalizedWord] = _wordFrequency.GetValueOrDefault(normalizedWord, 0) + 1;
        }

        // Track suggestion acceptance
        _suggestionAcceptance[suggestion] = _suggestionAcceptance.GetValueOrDefault(suggestion, 0) + 1;
    }

    public void RecordSuggestionRejected(string suggestion, string source)
    {
        _statistics.TotalSuggestionsRejected++;
        
        // Track rejection to improve accuracy
        _suggestionAcceptance[suggestion] = _suggestionAcceptance.GetValueOrDefault(suggestion, 0) - 1;
    }

    public void RecordKeystrokes(int count)
    {
        _statistics.TotalKeystrokes += count;
    }

    public void RecordTimeSaved(int milliseconds)
    {
        _statistics.TotalTimeSavedMs += milliseconds;
    }

    public void UpdateSessionDuration()
    {
        _statistics.SessionDuration = DateTime.UtcNow - _sessionStartTime;
    }

    public double GetAcceptanceRate()
    {
        if (_statistics.TotalSuggestionsShown == 0) return 0;
        return (double)_statistics.TotalSuggestionsAccepted / _statistics.TotalSuggestionsShown * 100;
    }

    public double GetAccuracyRate()
    {
        if (_statistics.TotalSuggestionsShown == 0) return 0;
        return 100 - ((double)_statistics.TotalSuggestionsRejected / _statistics.TotalSuggestionsShown * 100);
    }

    public Dictionary<string, int> GetTopWords(int count = 100)
    {
        return _wordFrequency
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public Dictionary<string, int> GetMostAcceptedSuggestions(int count = 50)
    {
        return _suggestionAcceptance
            .Where(kvp => kvp.Value > 0)
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void ResetSession()
    {
        _sessionStartTime = DateTime.UtcNow;
        _statistics.SessionSuggestionsShown = 0;
        _statistics.SessionSuggestionsAccepted = 0;
        _statistics.SessionKeystrokes = 0;
    }

    public LearningProgress GetProgress()
    {
        return new LearningProgress
        {
            AcceptanceRate = GetAcceptanceRate(),
            AccuracyRate = GetAccuracyRate(),
            WordsLearned = _wordFrequency.Count,
            SessionDuration = _statistics.SessionDuration,
            TimeSaved = TimeSpan.FromMilliseconds(_statistics.TotalTimeSavedMs),
            SuggestionsPerMinute = CalculateSuggestionsPerMinute()
        };
    }

    private double CalculateSuggestionsPerMinute()
    {
        var minutes = _statistics.SessionDuration.TotalMinutes;
        return minutes > 0 ? _statistics.SessionSuggestionsShown / minutes : 0;
    }
}

public class LearningStatistics
{
    public int TotalSuggestionsShown { get; set; }
    public int TotalSuggestionsAccepted { get; set; }
    public int TotalSuggestionsRejected { get; set; }
    public int DictionarySuggestionsShown { get; set; }
    public int DictionarySuggestionsAccepted { get; set; }
    public int LearnedSuggestionsShown { get; set; }
    public int LearnedSuggestionsAccepted { get; set; }
    public int AISuggestionsShown { get; set; }
    public int AISuggestionsAccepted { get; set; }
    public int TotalKeystrokes { get; set; }
    public long TotalTimeSavedMs { get; set; }
    
    // Session-specific stats
    public int SessionSuggestionsShown { get; set; }
    public int SessionSuggestionsAccepted { get; set; }
    public int SessionKeystrokes { get; set; }
    public TimeSpan SessionDuration { get; set; }
}

public class LearningProgress
{
    public double AcceptanceRate { get; set; }
    public double AccuracyRate { get; set; }
    public int WordsLearned { get; set; }
    public TimeSpan SessionDuration { get; set; }
    public TimeSpan TimeSaved { get; set; }
    public double SuggestionsPerMinute { get; set; }
}
