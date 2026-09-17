using System.Text.Json;

namespace Lexon.Privacy;

/// <summary>
/// Audit log system for suggestion requests and privacy monitoring
/// </summary>
public class AuditLog
{
    private readonly List<AuditEntry> _entries = new();
    private readonly object _lock = new();
    private const int MaxEntries = 10000;
    private string _logFilePath = string.Empty;

    public AuditLog(string? logDirectory = null)
    {
        InitializeLogPath(logDirectory);
    }

    private void InitializeLogPath(string? logDirectory)
    {
        if (string.IsNullOrEmpty(logDirectory))
        {
            logDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Lexon",
                "Logs"
            );
        }

        Directory.CreateDirectory(logDirectory);
        _logFilePath = Path.Combine(logDirectory, $"audit_{DateTime.UtcNow:yyyyMMdd}.log");
    }

    public void LogSuggestionRequest(string applicationName, string context, int contextLength, string? userId = null)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventType.SuggestionRequest,
            ApplicationName = applicationName,
            Context = context,
            ContextLength = contextLength,
            UserId = userId
        };

        AddEntry(entry);
    }

    public void LogSuggestionResponse(string requestId, string suggestion, string source, bool accepted)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventType.SuggestionResponse,
            RelatedRequestId = requestId,
            Suggestion = suggestion,
            Source = source,
            Accepted = accepted
        };

        AddEntry(entry);
    }

    public void LogSecureFieldBlocked(string applicationName, string fieldType)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventType.SecureFieldBlocked,
            ApplicationName = applicationName,
            FieldType = fieldType
        };

        AddEntry(entry);
    }

    public void LogApplicationBlocked(string applicationName, string reason)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventType.ApplicationBlocked,
            ApplicationName = applicationName,
            Reason = reason
        };

        AddEntry(entry);
    }

    public void LogDataTransmission(string provider, long bytesSent, long bytesReceived, bool success)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventType.DataTransmission,
            Provider = provider,
            BytesSent = bytesSent,
            BytesReceived = bytesReceived,
            Success = success
        };

        AddEntry(entry);
    }

    public void LogPrivacyViolation(string type, string description, string severity)
    {
        var entry = new AuditEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            EventType = AuditEventType.PrivacyViolation,
            ViolationType = type,
            Description = description,
            Severity = severity
        };

        AddEntry(entry);
    }

    private void AddEntry(AuditEntry entry)
    {
        lock (_lock)
        {
            _entries.Add(entry);

            // Trim entries if we exceed max
            while (_entries.Count > MaxEntries)
            {
                _entries.RemoveAt(0);
            }

            // Write to file asynchronously
            Task.Run(() => WriteEntryToFile(entry));
        }
    }

    private void WriteEntryToFile(AuditEntry entry)
    {
        try
        {
            var json = JsonSerializer.Serialize(entry);
            File.AppendAllText(_logFilePath, json + Environment.NewLine);
        }
        catch
        {
            // Silently fail if we can't write to log file
        }
    }

    public IEnumerable<AuditEntry> GetEntries(DateTime? start = null, DateTime? end = null)
    {
        lock (_lock)
        {
            var query = _entries.AsEnumerable();

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

    public IEnumerable<AuditEntry> GetEntriesByType(AuditEventType eventType)
    {
        lock (_lock)
        {
            return _entries.Where(e => e.EventType == eventType).ToList();
        }
    }

    public IEnumerable<AuditEntry> GetEntriesByApplication(string applicationName)
    {
        lock (_lock)
        {
            return _entries.Where(e => e.ApplicationName == applicationName).ToList();
        }
    }

    public AuditSummary GetSummary(TimeSpan period)
    {
        var cutoff = DateTime.UtcNow - period;
        var recentEntries = GetEntries(cutoff, null);

        return new AuditSummary
        {
            Period = period,
            TotalEntries = recentEntries.Count(),
            SuggestionRequests = recentEntries.Count(e => e.EventType == AuditEventType.SuggestionRequest),
            SuggestionsAccepted = recentEntries.Count(e => e.EventType == AuditEventType.SuggestionResponse && e.Accepted == true),
            SecureFieldsBlocked = recentEntries.Count(e => e.EventType == AuditEventType.SecureFieldBlocked),
            ApplicationsBlocked = recentEntries.Count(e => e.EventType == AuditEventType.ApplicationBlocked),
            DataTransmissions = recentEntries.Count(e => e.EventType == AuditEventType.DataTransmission),
            PrivacyViolations = recentEntries.Count(e => e.EventType == AuditEventType.PrivacyViolation),
            TopApplications = GetTopApplications(recentEntries),
            DataTransferred = recentEntries
                .Where(e => e.EventType == AuditEventType.DataTransmission)
                .Sum(e => e.BytesSent + e.BytesReceived)
        };
    }

    private Dictionary<string, int> GetTopApplications(IEnumerable<AuditEntry> entries)
    {
        return entries
            .Where(e => !string.IsNullOrEmpty(e.ApplicationName))
            .GroupBy(e => e.ApplicationName)
            .OrderByDescending(g => g.Count())
            .Take(10)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    public void ClearOldEntries(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        
        lock (_lock)
        {
            _entries.RemoveAll(e => e.Timestamp < cutoff);
        }
    }

    public void ClearAll()
    {
        lock (_lock)
        {
            _entries.Clear();
        }

        // Also clear old log files
        try
        {
            var logDirectory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(logDirectory))
            {
                var oldLogs = Directory.GetFiles(logDirectory, "audit_*.log")
                    .Where(f => File.GetCreationTime(f) < DateTime.UtcNow.AddDays(-30));
                
                foreach (var oldLog in oldLogs)
                {
                    File.Delete(oldLog);
                }
            }
        }
        catch
        {
            // Silently fail if we can't delete old logs
        }
    }
}

public class AuditEntry
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public AuditEventType EventType { get; set; }
    public string? ApplicationName { get; set; }
    public string? Context { get; set; }
    public int ContextLength { get; set; }
    public string? UserId { get; set; }
    public string? RelatedRequestId { get; set; }
    public string? Suggestion { get; set; }
    public string? Source { get; set; }
    public bool? Accepted { get; set; }
    public string? FieldType { get; set; }
    public string? Reason { get; set; }
    public string? Provider { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public bool? Success { get; set; }
    public string? ViolationType { get; set; }
    public string? Description { get; set; }
    public string? Severity { get; set; }
}

public enum AuditEventType
{
    SuggestionRequest,
    SuggestionResponse,
    SecureFieldBlocked,
    ApplicationBlocked,
    DataTransmission,
    PrivacyViolation,
    SettingsChanged,
    UserAction
}

public class AuditSummary
{
    public TimeSpan Period { get; set; }
    public int TotalEntries { get; set; }
    public int SuggestionRequests { get; set; }
    public int SuggestionsAccepted { get; set; }
    public int SecureFieldsBlocked { get; set; }
    public int ApplicationsBlocked { get; set; }
    public int DataTransmissions { get; set; }
    public int PrivacyViolations { get; set; }
    public Dictionary<string, int> TopApplications { get; set; } = new();
    public long DataTransferred { get; set; }
}
