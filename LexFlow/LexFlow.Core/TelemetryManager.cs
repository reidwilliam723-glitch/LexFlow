using System.Text;
using System.Text.Json;
using System.Reflection;
using System.Diagnostics;

namespace LexFlow.Core;

/// <summary>
/// Privacy-focused analytics and telemetry system
/// </summary>
public class TelemetryManager
{
    private readonly string _telemetryDirectory;
    private readonly string _telemetryFilePath;
    private readonly object _lock = new();
    private readonly List<TelemetryEvent> _events = new();
    private bool _isEnabled = false;
    private bool _isInitialized = false;

    // Singleton instance
    private static TelemetryManager? _instance;

    public static TelemetryManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new TelemetryManager();
            }
            return _instance;
        }
    }

    private TelemetryManager()
    {
        _telemetryDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LexFlow",
            "Telemetry"
        );

        Directory.CreateDirectory(_telemetryDirectory);
        _telemetryFilePath = Path.Combine(_telemetryDirectory, $"telemetry_{DateTime.UtcNow:yyyyMMdd}.json");
    }

    /// <summary>
    /// Initialize telemetry with user consent
    /// </summary>
    public void Initialize(bool userConsent)
    {
        _isEnabled = userConsent;
        _isInitialized = true;

        if (_isEnabled)
        {
            TrackEvent("Telemetry", "Initialized", new Dictionary<string, string>
            {
                { "Version", GetApplicationVersion() },
                { "OS", Environment.OSVersion.ToString() },
                { "Architecture", Environment.Is64BitProcess ? "x64" : "x86" }
            });
        }
    }

    /// <summary>
    /// Check if telemetry is enabled
    /// </summary>
    public bool IsEnabled => _isEnabled && _isInitialized;

    /// <summary>
    /// Enable or disable telemetry
    /// </summary>
    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
        if (enabled && !_isInitialized)
        {
            Initialize(true);
        }
    }

    /// <summary>
    /// Track a telemetry event
    /// </summary>
    public void TrackEvent(string category, string action, Dictionary<string, string>? properties = null)
    {
        if (!_isEnabled || !_isInitialized)
        {
            return;
        }

        try
        {
            var telemetryEvent = new TelemetryEvent
            {
                Id = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                Category = category,
                Action = action,
                Properties = properties ?? new Dictionary<string, string>(),
                SessionId = GetSessionId()
            };

            lock (_lock)
            {
                _events.Add(telemetryEvent);

                // Keep only last 1000 events in memory
                if (_events.Count > 1000)
                {
                    _events.RemoveAt(0);
                }

                // Persist events asynchronously
                Task.Run(() => PersistEvent(telemetryEvent));
            }
        }
        catch
        {
            // Silently fail if telemetry tracking fails
        }
    }

    /// <summary>
    /// Track a suggestion-related event
    /// </summary>
    public void TrackSuggestionEvent(string eventType, string source, bool accepted)
    {
        TrackEvent("Suggestions", eventType, new Dictionary<string, string>
        {
            { "Source", source },
            { "Accepted", accepted.ToString() }
        });
    }

    /// <summary>
    /// Track a feature usage event
    /// </summary>
    public void TrackFeatureUsage(string featureName)
    {
        TrackEvent("Features", "Used", new Dictionary<string, string>
        {
            { "Feature", featureName }
        });
    }

    /// <summary>
    /// Track an error event
    /// </summary>
    public void TrackError(string errorType, string errorMessage, string? context = null)
    {
        TrackEvent("Errors", errorType, new Dictionary<string, string>
        {
            { "Message", errorMessage },
            { "Context", context ?? "Unknown" }
        });
    }

    /// <summary>
    /// Track performance metrics
    /// </summary>
    public void TrackPerformance(string operation, long durationMs)
    {
        TrackEvent("Performance", operation, new Dictionary<string, string>
        {
            { "DurationMs", durationMs.ToString() }
        });
    }

    private void PersistEvent(TelemetryEvent telemetryEvent)
    {
        try
        {
            var json = JsonSerializer.Serialize(telemetryEvent);
            File.AppendAllText(_telemetryFilePath, json + Environment.NewLine);
        }
        catch
        {
            // Silently fail if persistence fails
        }
    }

    /// <summary>
    /// Get all telemetry events for analysis
    /// </summary>
    public IEnumerable<TelemetryEvent> GetEvents(DateTime? start = null, DateTime? end = null)
    {
        lock (_lock)
        {
            var query = _events.AsEnumerable();

            if (start.HasValue)
            {
                query = query.Where(e => e.Timestamp >= start.Value);
            }

            if (end.HasValue)
            {
                query = query.Where(e => e.Timestamp <= end.Value);
            }

            return query.ToList();
        }
    }

    /// <summary>
    /// Get telemetry summary for a time period
    /// </summary>
    public TelemetrySummary GetSummary(TimeSpan period)
    {
        var cutoff = DateTime.UtcNow - period;
        var recentEvents = GetEvents(cutoff, null);

        return new TelemetrySummary
        {
            Period = period,
            TotalEvents = recentEvents.Count(),
            EventTypes = recentEvents
                .GroupBy(e => e.Category)
                .ToDictionary(g => g.Key, g => g.Count()),
            FeatureUsage = recentEvents
                .Where(e => e.Category == "Features")
                .GroupBy(e => e.Properties.GetValueOrDefault("Feature", "Unknown"))
                .ToDictionary(g => g.Key, g => g.Count()),
            ErrorCount = recentEvents.Count(e => e.Category == "Errors"),
            PerformanceMetrics = recentEvents
                .Where(e => e.Category == "Performance")
                .ToDictionary(e => e.Action, e => long.Parse(e.Properties.GetValueOrDefault("DurationMs", "0")))
        };
    }

    /// <summary>
    /// Clear old telemetry data
    /// </summary>
    public void ClearOldData(TimeSpan maxAge)
    {
        try
        {
            var cutoff = DateTime.UtcNow - maxAge;

            lock (_lock)
            {
                _events.RemoveAll(e => e.Timestamp < cutoff);
            }

            // Clear old telemetry files
            var oldFiles = Directory.GetFiles(_telemetryDirectory, "telemetry_*.json")
                .Where(f => File.GetCreationTime(f) < cutoff);

            foreach (var oldFile in oldFiles)
            {
                try
                {
                    File.Delete(oldFile);
                }
                catch
                {
                    // Skip files we can't delete
                }
            }
        }
        catch
        {
            // Silently fail if cleanup fails
        }
    }

    /// <summary>
    /// Export telemetry data for analysis
    /// </summary>
    public string ExportTelemetry(DateTime? start = null, DateTime? end = null)
    {
        var events = GetEvents(start, end);
        return JsonSerializer.Serialize(events, new JsonSerializerOptions { WriteIndented = true });
    }

    /// <summary>
    /// Clear all telemetry data
    /// </summary>
    public void ClearAll()
    {
        lock (_lock)
        {
            _events.Clear();
        }

        try
        {
            var files = Directory.GetFiles(_telemetryDirectory, "telemetry_*.json");
            foreach (var file in files)
            {
                try
                {
                    File.Delete(file);
                }
                catch
                {
                    // Skip files we can't delete
                }
            }
        }
        catch
        {
            // Silently fail if cleanup fails
        }
    }

    private string GetApplicationVersion()
    {
        try
        {
            return Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Unknown";
        }
        catch
        {
            return "Unknown";
        }
    }

    private string GetSessionId()
    {
        // Generate a session ID based on process start time
        return Process.GetCurrentProcess().StartTime.ToString("yyyyMMdd_HHmmss");
    }
}

public class TelemetryEvent
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public Dictionary<string, string> Properties { get; set; } = new();
    public string SessionId { get; set; } = string.Empty;
}

public class TelemetrySummary
{
    public TimeSpan Period { get; set; }
    public int TotalEvents { get; set; }
    public Dictionary<string, int> EventTypes { get; set; } = new();
    public Dictionary<string, int> FeatureUsage { get; set; } = new();
    public int ErrorCount { get; set; }
    public Dictionary<string, long> PerformanceMetrics { get; set; } = new();
}