namespace LexFlow.Core.Learning;

/// <summary>
/// Collects manual rejection feedback to improve suggestion accuracy
/// </summary>
public class FeedbackCollector
{
    private readonly List<SuggestionFeedback> _feedback = new();
    private readonly Dictionary<string, FeedbackPattern> _patterns = new();

    public void RecordFeedback(string suggestion, string context, FeedbackType type, string? reason = null)
    {
        var feedback = new SuggestionFeedback
        {
            Suggestion = suggestion,
            Context = context,
            Type = type,
            Reason = reason,
            Timestamp = DateTime.UtcNow
        };

        _feedback.Add(feedback);
        
        // Analyze patterns
        AnalyzePattern(feedback);
        
        // Trim feedback history
        if (_feedback.Count > 1000)
        {
            _feedback.RemoveAt(0);
        }
    }

    private void AnalyzePattern(SuggestionFeedback feedback)
    {
        var patternKey = GetPatternKey(feedback);
        
        if (!_patterns.ContainsKey(patternKey))
        {
            _patterns[patternKey] = new FeedbackPattern();
        }
        
        var pattern = _patterns[patternKey];
        pattern.TotalCount++;
        
        if (feedback.Type == FeedbackType.Rejected)
        {
            pattern.RejectionCount++;
        }
        else if (feedback.Type == FeedbackType.Accepted)
        {
            pattern.AcceptanceCount++;
        }
    }

    private string GetPatternKey(SuggestionFeedback feedback)
    {
        // Create a pattern key based on context characteristics
        var lastWord = GetLastWord(feedback.Context);
        var suggestionLength = feedback.Suggestion.Length;
        var suggestionCategory = GetSuggestionCategory(feedback.Suggestion);
        
        return $"{lastWord}_{suggestionLength}_{suggestionCategory}";
    }

    private string GetLastWord(string context)
    {
        if (string.IsNullOrWhiteSpace(context)) return "empty";
        
        var words = context.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 ? words[^1].ToLowerInvariant() : "empty";
    }

    private string GetSuggestionCategory(string suggestion)
    {
        if (string.IsNullOrWhiteSpace(suggestion)) return "empty";
        
        if (suggestion.Length < 5) return "short";
        if (suggestion.Length < 15) return "medium";
        return "long";
    }

    public double GetRejectionRate(string suggestion)
    {
        var relevantFeedback = _feedback
            .Where(f => f.Suggestion.Equals(suggestion, StringComparison.OrdinalIgnoreCase))
            .ToList();
        
        if (relevantFeedback.Count == 0) return 0;
        
        var rejectedCount = relevantFeedback.Count(f => f.Type == FeedbackType.Rejected);
        return (double)rejectedCount / relevantFeedback.Count * 100;
    }

    public double GetRejectionRateForContext(string context)
    {
        var patternKey = GetPatternKey(new SuggestionFeedback { Context = context });
        
        if (!_patterns.ContainsKey(patternKey)) return 0;
        
        var pattern = _patterns[patternKey];
        return pattern.TotalCount > 0 ? (double)pattern.RejectionCount / pattern.TotalCount * 100 : 0;
    }

    public IEnumerable<SuggestionFeedback> GetRecentFeedback(int count = 50)
    {
        return _feedback.TakeLast(count);
    }

    public Dictionary<string, double> GetProblematicSuggestions(double threshold = 50)
    {
        var suggestionRejectionRates = new Dictionary<string, double>();
        
        var groupedFeedback = _feedback
            .GroupBy(f => f.Suggestion.ToLowerInvariant());
        
        foreach (var group in groupedFeedback)
        {
            var rejectionRate = (double)group.Count(f => f.Type == FeedbackType.Rejected) / group.Count() * 100;
            
            if (rejectionRate >= threshold)
            {
                suggestionRejectionRates[group.Key] = rejectionRate;
            }
        }
        
        return suggestionRejectionRates.OrderByDescending(kvp => kvp.Value)
                                     .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public void ClearOldFeedback(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        _feedback.RemoveAll(f => f.Timestamp < cutoff);
    }
}

public class SuggestionFeedback
{
    public string Suggestion { get; set; } = string.Empty;
    public string Context { get; set; } = string.Empty;
    public FeedbackType Type { get; set; }
    public string? Reason { get; set; }
    public DateTime Timestamp { get; set; }
}

public enum FeedbackType
{
    Accepted,
    Rejected,
    Modified,
    Ignored
}

public class FeedbackPattern
{
    public int TotalCount { get; set; }
    public int AcceptanceCount { get; set; }
    public int RejectionCount { get; set; }
}
