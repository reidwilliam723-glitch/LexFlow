using LexFlow.Storage;
using Xunit;

namespace LexFlow.Tests;

public class EncryptedStorageTests
{
    private readonly string _testPath;

    public EncryptedStorageTests()
    {
        _testPath = Path.Combine(Path.GetTempPath(), $"LexFlowTest_{Guid.NewGuid()}");
        if (!Directory.Exists(_testPath))
        {
            Directory.CreateDirectory(_testPath);
        }
    }

    private void Cleanup()
    {
        if (Directory.Exists(_testPath))
        {
            try
            {
                Directory.Delete(_testPath, true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }
    }

    [Fact]
    public async Task SaveAsync_LoadAsync_RoundTrip_ReturnsOriginalValue()
    {
        // Arrange
        var storage = new EncryptedStorage(_testPath);
        var key = "test-key";
        var value = "test-value";

        // Act
        await storage.SaveAsync(key, value);
        var retrieved = await storage.LoadAsync<string>(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task SaveAsync_LoadAsync_WithDifferentInstance_CanReadData()
    {
        // Arrange
        var storage1 = new EncryptedStorage(_testPath);
        var storage2 = new EncryptedStorage(_testPath);
        var key = "test-key";
        var value = "test-value";

        // Act
        await storage1.SaveAsync(key, value);
        var retrieved = await storage2.LoadAsync<string>(key);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public async Task LoadAsync_NonExistentKey_ReturnsNull()
    {
        // Arrange
        var storage = new EncryptedStorage(_testPath);

        // Act
        var result = await storage.LoadAsync<string>("non-existent-key");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingValue()
    {
        // Arrange
        var storage = new EncryptedStorage(_testPath);
        var key = "test-key";
        await storage.SaveAsync(key, "original");

        // Act
        await storage.SaveAsync(key, "modified");
        var retrieved = await storage.LoadAsync<string>(key);

        // Assert
        Assert.Equal("modified", retrieved);
    }

    [Fact]
    public async Task ExistsAsync_ExistingKey_ReturnsTrue()
    {
        // Arrange
        var storage = new EncryptedStorage(_testPath);
        var key = "test-key";
        await storage.SaveAsync(key, "value");

        // Act
        var exists = await storage.ExistsAsync(key);

        // Assert
        Assert.True(exists);
    }

    [Fact]
    public async Task ExistsAsync_NonExistentKey_ReturnsFalse()
    {
        // Arrange
        var storage = new EncryptedStorage(_testPath);

        // Act
        var exists = await storage.ExistsAsync("non-existent-key");

        // Assert
        Assert.False(exists);
    }

    [Fact]
    public async Task DeleteAsync_NonExistentKey_DoesNotThrow()
    {
        // Arrange
        var storage = new EncryptedStorage(_testPath);

        // Act & Assert
        await storage.DeleteAsync("non-existent-key");
    }
}
