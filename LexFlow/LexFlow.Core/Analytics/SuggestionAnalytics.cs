using LexFlow.Core.History;
using LexFlow.Core.Learning;

namespace LexFlow.Core.Analytics;

/// <summary>
/// Provides analytics and statistics for suggestion usage
/// </summary>
public class SuggestionAnalytics
{
    private readonly SuggestionHistory _history;
    private readonly LearningProgressTracker _learningTracker;
    private readonly FeedbackCollector _feedbackCollector;

    public SuggestionAnalytics(
        SuggestionHistory history,
        LearningProgressTracker learningTracker,
        FeedbackCollector feedbackCollector)
    {
        _history = history ?? throw new ArgumentNullException(nameof(history));
        _learningTracker = learningTracker ?? throw new ArgumentNullException(nameof(learningTracker));
        _feedbackCollector = feedbackCollector ?? throw new ArgumentNullException(nameof(feedbackCollector));
    }

    public AnalyticsReport GenerateReport(TimeSpan period)
    {
        var cutoff = DateTime.UtcNow - period;
        var recentHistory = _history.GetRecentEntries(100)
            .Where(entry => entry.Timestamp >= cutoff)
            .ToList();

        var report = new AnalyticsReport
        {
            Period = period,
            GeneratedAt = DateTime.UtcNow,
            TotalSuggestions = recentHistory.Count,
            LearningProgress = _learningTracker.GetProgress(),
            TopApplications = GetTopApplications(recentHistory),
            SuggestionSources = GetSuggestionSourceBreakdown(recentHistory),
            TimeDistribution = GetTimeDistribution(recentHistory),
            ProblematicSuggestions = _feedbackCollector.GetProblematicSuggestions(30)
        };

        return report;
    }

    private Dictionary<string, int> GetTopApplications(List<SuggestionHistoryEntry> entries)
    {
        return entries
            .GroupBy(e => e.ApplicationName)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private Dictionary<string, int> GetSuggestionSourceBreakdown(List<SuggestionHistoryEntry> entries)
    {
        return entries
            .GroupBy(e => e.Suggestion.Source)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    private Dictionary<string, int> GetTimeDistribution(List<SuggestionHistoryEntry> entries)
    {
        return entries
            .GroupBy(e => e.Timestamp.Hour)
            .OrderBy(g => g.Key)
            .ToDictionary(g => $"{g.Key}:00", g => g.Count());
    }

    public UsageTrends GetUsageTrends(int days = 7)
    {
        var trends = new UsageTrends();
        var dailyData = new Dictionary<DateTime, DailyUsage>();

        for (int i = days - 1; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            var dayStart = date;
            var dayEnd = date.AddDays(1);

            var dayHistory = _history.GetRecentEntries(1000)
                .Where(e => e.Timestamp >= dayStart && e.Timestamp < dayEnd)
                .ToList();

            dailyData[date] = new DailyUsage
            {
                Date = date,
                SuggestionsShown = dayHistory.Count,
                SuggestionsAccepted = dayHistory.Count(e => e.Accepted),
                UniqueApplications = dayHistory.Select(e => e.ApplicationName).Distinct().Count()
            };
        }

        trends.DailyData = dailyData;
        trends.AverageDailySuggestions = dailyData.Values.Average(d => d.SuggestionsShown);
        trends.TrendDirection = CalculateTrendDirection(dailyData.Values.ToList());

        return trends;
    }

    private TrendDirection CalculateTrendDirection(List<DailyUsage> dailyData)
    {
        if (dailyData.Count < 2) return TrendDirection.Stable;

        var recent = dailyData.TakeLast(3).Average(d => d.SuggestionsShown);
        var earlier = dailyData.Take(3).Average(d => d.SuggestionsShown);

        if (recent > earlier * 1.1) return TrendDirection.Increasing;
        if (recent < earlier * 0.9) return TrendDirection.Decreasing;
        return TrendDirection.Stable;
    }
}

public class AnalyticsReport
{
    public TimeSpan Period { get; set; }
    public DateTime GeneratedAt { get; set; }
    public int TotalSuggestions { get; set; }
    public LearningProgress LearningProgress { get; set; } = null!;
    public Dictionary<string, int> TopApplications { get; set; } = new();
    public Dictionary<string, int> SuggestionSources { get; set; } = new();
    public Dictionary<string, int> TimeDistribution { get; set; } = new();
    public Dictionary<string, double> ProblematicSuggestions { get; set; } = new();
}

public class UsageTrends
{
    public Dictionary<DateTime, DailyUsage> DailyData { get; set; } = new();
    public double AverageDailySuggestions { get; set; }
    public TrendDirection TrendDirection { get; set; }
}

public class DailyUsage
{
    public DateTime Date { get; set; }
    public int SuggestionsShown { get; set; }
    public int SuggestionsAccepted { get; set; }
    public int UniqueApplications { get; set; }
}

public enum TrendDirection
{
    Increasing,
    Decreasing,
    Stable
}
