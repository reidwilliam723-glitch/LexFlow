using System.Diagnostics;

namespace LexFlow.Service;

/// <summary>
/// Monitors system resource usage
/// </summary>
public class ResourceMonitor
{
    private readonly PerformanceCounter _cpuCounter;
    private readonly PerformanceCounter _memoryCounter;
    private readonly System.Threading.Timer _updateTimer;
    private ResourceUsage _currentUsage = new();
    
    public event EventHandler<ResourceUsageEventArgs>? UsageUpdated;

    public ResourceMonitor()
    {
        _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        _memoryCounter = new PerformanceCounter("Memory", "Available MBytes");
        
        _updateTimer = new System.Threading.Timer(OnUpdateTimer, null, 1000, 1000);
    }

    public void Start()
    {
        // Timer is already started in constructor
    }

    public void Stop()
    {
        _updateTimer?.Dispose();
    }

    private void OnUpdateTimer(object? state)
    {
        try
        {
            _currentUsage = new ResourceUsage
            {
                Timestamp = DateTime.UtcNow,
                CPUPercent = _cpuCounter.NextValue(),
                AvailableMemoryMB = _memoryCounter.NextValue(),
                TotalMemoryMB = GetTotalMemoryMB(),
                UsedMemoryMB = GetTotalMemoryMB() - _memoryCounter.NextValue(),
                MemoryPercent = CalculateMemoryPercent()
            };

            UsageUpdated?.Invoke(this, new ResourceUsageEventArgs { Usage = _currentUsage });
        }
        catch
        {
            // Silently fail if counters are unavailable
        }
    }

    private float GetTotalMemoryMB()
    {
        using var proc = Process.GetCurrentProcess();
        return proc.PrivateMemorySize64 / (1024 * 1024);
    }

    private float CalculateMemoryPercent()
    {
        if (_currentUsage.TotalMemoryMB == 0) return 0;
        return (_currentUsage.UsedMemoryMB / _currentUsage.TotalMemoryMB) * 100;
    }

    public ResourceUsage GetCurrentUsage()
    {
        return _currentUsage;
    }

    public ResourceHistory GetHistory(TimeSpan period)
    {
        // This would require storing historical data
        // For now, return current usage as a single point
        return new ResourceHistory
        {
            Period = period,
            AverageCPU = _currentUsage.CPUPercent,
            AverageMemory = _currentUsage.MemoryPercent,
            PeakCPU = _currentUsage.CPUPercent,
            PeakMemory = _currentUsage.MemoryPercent
        };
    }

    public bool IsResourceUsageHigh(float cpuThreshold = 80, float memoryThreshold = 80)
    {
        return _currentUsage.CPUPercent > cpuThreshold || _currentUsage.MemoryPercent > memoryThreshold;
    }

    public void Dispose()
    {
        _cpuCounter?.Dispose();
        _memoryCounter?.Dispose();
        _updateTimer?.Dispose();
    }
}

public class ResourceUsage
{
    public DateTime Timestamp { get; set; }
    public float CPUPercent { get; set; }
    public float AvailableMemoryMB { get; set; }
    public float TotalMemoryMB { get; set; }
    public float UsedMemoryMB { get; set; }
    public float MemoryPercent { get; set; }
    public int ThreadCount { get; set; }
    public long HandleCount { get; set; }
}

public class ResourceHistory
{
    public TimeSpan Period { get; set; }
    public float AverageCPU { get; set; }
    public float AverageMemory { get; set; }
    public float PeakCPU { get; set; }
    public float PeakMemory { get; set; }
}

public class ResourceUsageEventArgs : EventArgs
{
    public ResourceUsage Usage { get; set; } = null!;
}
