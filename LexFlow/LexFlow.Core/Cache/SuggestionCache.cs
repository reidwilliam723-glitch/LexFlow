using LexFlow.Core.Models;

namespace LexFlow.Core.Cache;

/// <summary>
/// Suggestion cache for offline operation
/// </summary>
public class SuggestionCache
{
    private readonly Dictionary<string, CachedSuggestions> _cache = new();
    private readonly LinkedList<string> _accessOrder = new();
    private readonly int _maxCacheSize;
    private readonly TimeSpan _cacheExpiration;

    public SuggestionCache(int maxCacheSize = 1000, TimeSpan? cacheExpiration = null)
    {
        _maxCacheSize = maxCacheSize;
        _cacheExpiration = cacheExpiration ?? TimeSpan.FromHours(24);
    }

    public void CacheSuggestions(string context, IEnumerable<Suggestion> suggestions, string source)
    {
        var cacheKey = GenerateCacheKey(context);
        
        var cached = new CachedSuggestions
        {
            Context = context,
            Suggestions = suggestions.ToList(),
            Source = source,
            CachedAt = DateTime.UtcNow,
            AccessCount = 0
        };

        _cache[cacheKey] = cached;
        UpdateAccessOrder(cacheKey);
        
        // Trim cache if needed
        TrimCache();
    }

    public IEnumerable<Suggestion>? GetCachedSuggestions(string context)
    {
        var cacheKey = GenerateCacheKey(context);
        
        if (!_cache.TryGetValue(cacheKey, out var cached))
        {
            return null;
        }

        // Check if expired
        if (DateTime.UtcNow - cached.CachedAt > _cacheExpiration)
        {
            _cache.Remove(cacheKey);
            _accessOrder.Remove(cacheKey);
            return null;
        }

        // Update access
        cached.AccessCount++;
        cached.LastAccessed = DateTime.UtcNow;
        UpdateAccessOrder(cacheKey);

        return cached.Suggestions;
    }

    public void InvalidateCache(string context)
    {
        var cacheKey = GenerateCacheKey(context);
        _cache.Remove(cacheKey);
        _accessOrder.Remove(cacheKey);
    }

    public void ClearCache()
    {
        _cache.Clear();
        _accessOrder.Clear();
    }

    public void ClearExpiredCache()
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

    private string GenerateCacheKey(string context)
    {
        // Normalize context for caching
        var normalized = context.ToLowerInvariant().Trim();
        // Take last few words for context matching
        var words = normalized.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var lastWords = words.Length > 5 ? words[^5..] : words;
        return string.Join(" ", lastWords);
    }

    public CacheStatistics GetStatistics()
    {
        return new CacheStatistics
        {
            TotalEntries = _cache.Count,
            TotalAccessCount = _cache.Values.Sum(c => c.AccessCount),
            CacheSize = CalculateCacheSize(),
            HitRate = CalculateHitRate(),
            OldestEntry = _cache.Values.Min(c => c.CachedAt),
            NewestEntry = _cache.Values.Max(c => c.CachedAt)
        };
    }

    private long CalculateCacheSize()
    {
        // Rough estimate of cache size in bytes
        return _cache.Values.Sum(c => 
            c.Context.Length * 2 + 
            c.Suggestions.Sum(s => s.Text.Length * 2) +
            c.Source.Length * 2);
    }

    private double CalculateHitRate()
    {
        // This would need tracking of hits vs misses
        // For now, return a placeholder
        return 0.0;
    }

    public IEnumerable<CachedSuggestions> GetMostAccessed(int count = 10)
    {
        return _cache.Values
            .OrderByDescending(c => c.AccessCount)
            .Take(count);
    }
}

public class CachedSuggestions
{
    public string Context { get; set; } = string.Empty;
    public List<Suggestion> Suggestions { get; set; } = new();
    public string Source { get; set; } = string.Empty;
    public DateTime CachedAt { get; set; }
    public DateTime LastAccessed { get; set; }
    public int AccessCount { get; set; }
}

public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int TotalAccessCount { get; set; }
    public long CacheSize { get; set; }
    public double HitRate { get; set; }
    public DateTime OldestEntry { get; set; }
    public DateTime NewestEntry { get; set; }
}
