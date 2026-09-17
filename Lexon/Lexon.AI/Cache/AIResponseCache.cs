namespace Lexon.AI.Cache;

/// <summary>
/// Caches AI responses for rewriting, grammar improvement, and tone changes
/// </summary>
public class AIResponseCache
{
    private readonly Dictionary<string, CachedAIResponse> _cache = new();
    private readonly LinkedList<string> _accessOrder = new();
    private readonly int _maxCacheSize;
    private readonly TimeSpan _cacheExpiration;
    private readonly object _lock = new();
    private int _hitCount;
    private int _missCount;

    public AIResponseCache(int maxCacheSize = 500, TimeSpan? cacheExpiration = null)
    {
        _maxCacheSize = maxCacheSize;
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromHours(1); // AI responses expire faster
    }

    public void CacheResponse(string operation, string input, string response)
    {
        var cacheKey = GenerateCacheKey(operation, input);
        
        var cached = new CachedAIResponse
        {
            Operation = operation,
            Input = input,
            Response = response,
            CachedAt = DateTime.UtcNow,
            AccessCount = 0
        };

        lock (_lock)
        {
            _cache[cacheKey] = cached;
            UpdateAccessOrder(cacheKey);
            TrimCache();
        }
    }

    public string? GetCachedResponse(string operation, string input)
    {
        var cacheKey = GenerateCacheKey(operation, input);

        lock (_lock)
        {
            if (!_cache.TryGetValue(cacheKey, out var cached))
            {
                _missCount++;
                return null;
            }

            // Check if expired
            if (DateTime.UtcNow - cached.CachedAt > _cacheExpiration)
            {
                _cache.Remove(cacheKey);
                _accessOrder.Remove(cacheKey);
                _missCount++;
                return null;
            }

            // Update access
            _hitCount++;
            cached.AccessCount++;
            cached.LastAccessed = DateTime.UtcNow;
            UpdateAccessOrder(cacheKey);

            return cached.Response;
        }
    }

    public void InvalidateCache(string operation, string input)
    {
        var cacheKey = GenerateCacheKey(operation, input);
        lock (_lock)
        {
            _cache.Remove(cacheKey);
            _accessOrder.Remove(cacheKey);
        }
    }

    public void InvalidateOperation(string operation)
    {
        lock (_lock)
        {
            var keysToRemove = _cache
                .Where(kvp => kvp.Value.Operation == operation)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in keysToRemove)
            {
                _cache.Remove(key);
                _accessOrder.Remove(key);
            }
        }
    }

    public void ClearCache()
    {
        lock (_lock)
        {
            _cache.Clear();
            _accessOrder.Clear();
            _hitCount = 0;
            _missCount = 0;
        }
    }

    public void ClearExpiredCache()
    {
        lock (_lock)
        {
            var expiredKeys = _cache
                .Where(kvp => DateTime.UtcNow - kvp.Value.CachedAt > _cacheExpiration)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in expiredKeys)
            {
                _cache.Remove(key);
                _accessOrder.Remove(key);
            }
        }
    }

    private void TrimCache()
    {
        while (_cache.Count > _maxCacheSize)
        {
            var oldestKey = _accessOrder.First.Value;
            _cache.Remove(oldestKey);
            _accessOrder.RemoveFirst();
        }
    }

    private void UpdateAccessOrder(string cacheKey)
    {
        _accessOrder.Remove(cacheKey);
        _accessOrder.AddLast(cacheKey);
    }

    private string GenerateCacheKey(string operation, string input)
    {
        // Use hash of operation + normalized input for cache key
        var normalizedInput = input.ToLowerInvariant().Trim();
        var combined = $"{operation}:{normalizedInput}";
        return System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(combined))
            .Take(8)
            .Aggregate("", (current, b) => current + b.ToString("x2"));
    }

    public AIResponseCacheStatistics GetStatistics()
    {
        lock (_lock)
        {
            var totalRequests = _hitCount + _missCount;
            return new AIResponseCacheStatistics
            {
                TotalEntries = _cache.Count,
                TotalAccessCount = _cache.Values.Sum(c => c.AccessCount),
                CacheSize = CalculateCacheSize(),
                HitRate = totalRequests > 0 ? (double)_hitCount / totalRequests : 0.0,
                HitCount = _hitCount,
                MissCount = _missCount,
                OldestEntry = _cache.Count > 0 ? _cache.Values.Min(c => c.CachedAt) : DateTime.MinValue,
                NewestEntry = _cache.Count > 0 ? _cache.Values.Max(c => c.CachedAt) : DateTime.MinValue
            };
        }
    }

    private long CalculateCacheSize()
    {
        return _cache.Values.Sum(c => 
            c.Operation.Length * 2 + 
            c.Input.Length * 2 + 
            c.Response.Length * 2);
    }

    public IEnumerable<CachedAIResponse> GetMostAccessed(int count = 10)
    {
        lock (_lock)
        {
            return _cache.Values
                .OrderByDescending(c => c.AccessCount)
                .Take(count)
                .ToList();
        }
    }
}

public class CachedAIResponse
{
    public string Operation { get; set; } = string.Empty;
    public string Input { get; set; } = string.Empty;
    public string Response { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime LastAccessed { get; set; }
    public int AccessCount { get; set; }
}

public class AIResponseCacheStatistics
{
    public int TotalEntries { get; set; }
    public int TotalAccessCount { get; set; }
    public long CacheSize { get; set; }
    public double HitRate { get; set; }
    public int HitCount { get; set; }
    public int MissCount { get; set; }
    public DateTime OldestEntry { get; set; }
    public DateTime NewestEntry { get; set; }
}
