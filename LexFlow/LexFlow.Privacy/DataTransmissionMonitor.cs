namespace LexFlow.Privacy;

/// <summary>
/// Monitors data transmission for cloud AI services
/// </summary>
public class DataTransmissionMonitor
{
    private readonly List<TransmissionRecord> _transmissions = new();
    private readonly Dictionary<string, TransmissionStats> _providerStats = new();
    private long _totalBytesSent = 0;
    private long _totalBytesReceived = 0;

    public event EventHandler<TransmissionEventArgs>? TransmissionStarted;
    public event EventHandler<TransmissionEventArgs>? TransmissionCompleted;
    public event EventHandler<TransmissionEventArgs>? TransmissionFailed;

    public bool IsTransmitting { get; private set; }

    public void RecordTransmissionStart(string provider, string dataType, long estimatedSize)
    {
        var record = new TransmissionRecord
        {
            Id = Guid.NewGuid().ToString(),
            Provider = provider,
            DataType = dataType,
            StartTime = DateTime.UtcNow,
            EstimatedSize = estimatedSize,
            Status = TransmissionStatus.InProgress
        };

        _transmissions.Add(record);

        if (!_providerStats.ContainsKey(provider))
        {
            _providerStats[provider] = new TransmissionStats();
        }

        IsTransmitting = true;
        TransmissionStarted?.Invoke(this, new TransmissionEventArgs { Record = record });
    }

    public void RecordTransmissionComplete(string transmissionId, long bytesSent, long bytesReceived)
    {
        var record = _transmissions.FirstOrDefault(t => t.Id == transmissionId);
        if (record == null) return;

        record.EndTime = DateTime.UtcNow;
        record.BytesSent = bytesSent;
        record.BytesReceived = bytesReceived;
        record.Status = TransmissionStatus.Completed;

        _totalBytesSent += bytesSent;
        _totalBytesReceived += bytesReceived;

        if (_providerStats.TryGetValue(record.Provider, out var stats))
        {
            stats.TotalTransmissions++;
            stats.TotalBytesSent += bytesSent;
            stats.TotalBytesReceived += bytesReceived;
            stats.LastTransmissionTime = DateTime.UtcNow;
        }

        IsTransmitting = _transmissions.Any(t => t.Status == TransmissionStatus.InProgress);
        TransmissionCompleted?.Invoke(this, new TransmissionEventArgs { Record = record });
    }

    public void RecordTransmissionFailure(string transmissionId, string error)
    {
        var record = _transmissions.FirstOrDefault(t => t.Id == transmissionId);
        if (record == null) return;

        record.EndTime = DateTime.UtcNow;
        record.Status = TransmissionStatus.Failed;
        record.Error = error;

        if (_providerStats.TryGetValue(record.Provider, out var stats))
        {
            stats.FailedTransmissions++;
        }

        IsTransmitting = _transmissions.Any(t => t.Status == TransmissionStatus.InProgress);
        TransmissionFailed?.Invoke(this, new TransmissionEventArgs { Record = record });
    }

    public TransmissionStats GetProviderStats(string provider)
    {
        return _providerStats.TryGetValue(provider, out var stats) ? stats : new TransmissionStats();
    }

    public IEnumerable<TransmissionRecord> GetRecentTransmissions(int count = 50)
    {
        return _transmissions.TakeLast(count);
    }

    public long GetTotalBytesSent() => _totalBytesSent;
    public long GetTotalBytesReceived() => _totalBytesReceived;

    public void ClearOldRecords(TimeSpan maxAge)
    {
        var cutoff = DateTime.UtcNow - maxAge;
        _transmissions.RemoveAll(t => t.StartTime < cutoff);
    }

    public void ClearAllRecords()
    {
        _transmissions.Clear();
        _totalBytesSent = 0;
        _totalBytesReceived = 0;
    }
}

public class TransmissionRecord
{
    public string Id { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }
    public long EstimatedSize { get; set; }
    public long BytesSent { get; set; }
    public long BytesReceived { get; set; }
    public TransmissionStatus Status { get; set; }
    public string? Error { get; set; }
}

public class TransmissionStats
{
    public int TotalTransmissions { get; set; }
    public int FailedTransmissions { get; set; }
    public long TotalBytesSent { get; set; }
    public long TotalBytesReceived { get; set; }
    public DateTime? LastTransmissionTime { get; set; }

    public double SuccessRate => TotalTransmissions > 0 
        ? (double)(TotalTransmissions - FailedTransmissions) / TotalTransmissions * 100 
        : 0;
}

public enum TransmissionStatus
{
    InProgress,
    Completed,
    Failed
}

public class TransmissionEventArgs : EventArgs
{
    public TransmissionRecord Record { get; set; } = null!;
}
