using Lexon.AI.Cache;
using Xunit;

namespace Lexon.Tests;

public class AIResponseCacheTests
{
    [Fact]
    public void CacheResponse_GetCachedResponse_ReturnsCachedValue()
    {
        // Arrange
        var cache = new AIResponseCache(10, TimeSpan.FromHours(1));
        var operation = "rewrite";
        var input = "test input";
        var response = "cached-response";

        // Act
        cache.CacheResponse(operation, input, response);
        var result = cache.GetCachedResponse(operation, input);

        // Assert
        Assert.Equal(response, result);
    }

    [Fact]
    public void GetCachedResponse_CacheMiss_ReturnsNull()
    {
        // Arrange
        var cache = new AIResponseCache(10, TimeSpan.FromHours(1));

        // Act
        var result = cache.GetCachedResponse("non-existent", "input");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void CacheResponse_WhenCacheFull_EvictsOldestEntry()
    {
        // Arrange
        var cache = new AIResponseCache(3, TimeSpan.FromHours(1));
        cache.CacheResponse("op1", "input1", "value1");
        cache.CacheResponse("op2", "input2", "value2");
        cache.CacheResponse("op3", "input3", "value3");

        // Act
        cache.CacheResponse("op4", "input4", "value4"); // Should evict op1

        // Assert
        Assert.Null(cache.GetCachedResponse("op1", "input1"));
        Assert.Equal("value2", cache.GetCachedResponse("op2", "input2"));
        Assert.Equal("value3", cache.GetCachedResponse("op3", "input3"));
        Assert.Equal("value4", cache.GetCachedResponse("op4", "input4"));
    }

    [Fact]
    public void CacheResponse_ExistingKey_UpdatesValue()
    {
        // Arrange
        var cache = new AIResponseCache(10, TimeSpan.FromHours(1));
        cache.CacheResponse("op1", "input1", "value1");

        // Act
        cache.CacheResponse("op1", "input1", "value2");

        // Assert
        Assert.Equal("value2", cache.GetCachedResponse("op1", "input1"));
    }

    [Fact]
    public void CacheResponse_ExistingKey_UpdatesExpiry()
    {
        // Arrange
        var cache = new AIResponseCache(10, TimeSpan.FromMilliseconds(200));
        cache.CacheResponse("op1", "input1", "value1");

        // Act
        System.Threading.Thread.Sleep(100);
        cache.CacheResponse("op1", "input1", "value2"); // Should refresh expiry
        System.Threading.Thread.Sleep(150); // Original expiry would have been 200ms total, but refreshed to 200ms from update

        // Assert
        Assert.Equal("value2", cache.GetCachedResponse("op1", "input1")); // Should still be there
    }

    [Fact]
    public void GetCachedResponse_ExpiredEntry_ReturnsNull()
    {
        // Arrange
        var cache = new AIResponseCache(10, TimeSpan.FromMilliseconds(50));
        cache.CacheResponse("op1", "input1", "value1");

        // Act
        System.Threading.Thread.Sleep(60);
        var result = cache.GetCachedResponse("op1", "input1");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void InvalidateOperation_RemovesAllEntriesForOperation()
    {
        // Arrange
        var cache = new AIResponseCache(10, TimeSpan.FromHours(1));
        cache.CacheResponse("op1", "input1", "value1");
        cache.CacheResponse("op1", "input2", "value2");
        cache.CacheResponse("op2", "input1", "value3");

        // Act
        cache.InvalidateOperation("op1");

        // Assert
        Assert.Null(cache.GetCachedResponse("op1", "input1"));
        Assert.Null(cache.GetCachedResponse("op1", "input2"));
        Assert.Equal("value3", cache.GetCachedResponse("op2", "input1"));
    }

    [Fact]
    public void InvalidateCache_RemovesSpecificEntry()
    {
        // Arrange
        var cache = new AIResponseCache(10, TimeSpan.FromHours(1));
        cache.CacheResponse("op1", "input1", "value1");
        cache.CacheResponse("op2", "input2", "value2");

        // Act
        cache.InvalidateCache("op1", "input1");

        // Assert
        Assert.Null(cache.GetCachedResponse("op1", "input1"));
        Assert.Equal("value2", cache.GetCachedResponse("op2", "input2"));
    }

    [Fact]
    public void InvalidateCache_NonExistentKey_DoesNotThrow()
    {
        // Arrange
        var cache = new AIResponseCache(10, TimeSpan.FromHours(1));

        // Act & Assert
        cache.InvalidateCache("non-existent", "input");
    }

    [Fact]
    public void Clear_RemovesAllEntries()
    {
        // Arrange
        var cache = new AIResponseCache(10, TimeSpan.FromHours(1));
        cache.CacheResponse("op1", "input1", "value1");
        cache.CacheResponse("op2", "input2", "value2");

        // Act
        cache.ClearCache();

        // Assert
        Assert.Null(cache.GetCachedResponse("op1", "input1"));
        Assert.Null(cache.GetCachedResponse("op2", "input2"));
    }

    [Fact]
    public void GetStatistics_ReturnsCorrectStats()
    {
        // Arrange
        var cache = new AIResponseCache(10, TimeSpan.FromHours(1));
        cache.CacheResponse("op1", "input1", "value1");
        cache.CacheResponse("op2", "input2", "value2");

        // Act
        cache.GetCachedResponse("op1", "input1"); // Hit
        cache.GetCachedResponse("op3", "input3"); // Miss
        var stats = cache.GetStatistics();

        // Assert
        Assert.Equal(1, stats.HitCount);
        Assert.Equal(1, stats.MissCount);
    }
}
