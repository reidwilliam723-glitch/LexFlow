using LexFlow.Core.Interfaces;
using LexFlow.Profiles;
using Moq;
using System.Text.Json;
using Xunit;

namespace LexFlow.Tests;

public class ProfileTests
{
    private readonly Mock<IStorage> _mockStorage;

    public ProfileTests()
    {
        _mockStorage = new Mock<IStorage>();
    }

    [Fact]
    public void SetSetting_GetSetting_Bool_RoundTrip()
    {
        // Arrange
        var profile = new Profile(_mockStorage.Object);
        var key = "TestBool";
        var value = true;

        // Act
        profile.SetSetting(key, value);
        var retrieved = profile.GetSetting(key, false);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public void SetSetting_GetSetting_String_RoundTrip()
    {
        // Arrange
        var profile = new Profile(_mockStorage.Object);
        var key = "TestString";
        var value = "test-value";

        // Act
        profile.SetSetting(key, value);
        var retrieved = profile.GetSetting(key, "default");

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public void SetSetting_GetSetting_ListString_RoundTrip()
    {
        // Arrange
        var profile = new Profile(_mockStorage.Object);
        var key = "TestList";
        var value = new List<string> { "item1", "item2", "item3" };

        // Act
        profile.SetSetting(key, value);
        var retrieved = profile.GetSetting<List<string>>(key, new List<string>());

        // Assert
        Assert.Equal(value, retrieved);
        Assert.Equal(3, retrieved.Count);
        Assert.Contains("item1", retrieved);
        Assert.Contains("item2", retrieved);
        Assert.Contains("item3", retrieved);
    }

    [Fact]
    public async Task SetSetting_GetSetting_ThroughSaveLoad_RoundTrip()
    {
        // Arrange
        var profile = new Profile(_mockStorage.Object);
        var key = "TestListSerialization";
        var value = new List<string> { "app1", "app2", "app3" };

        // Act
        profile.SetSetting(key, value);
        
        // Simulate save/load through storage
        _mockStorage.Setup(s => s.LoadAsync<Profile>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(profile);
        
        await profile.SaveAsync();
        await profile.LoadAsync();
        
        var retrieved = profile.GetSetting<List<string>>(key, new List<string>());

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(3, retrieved.Count);
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public void GetSetting_WhenKeyNotSet_ReturnsDefault()
    {
        // Arrange
        var profile = new Profile(_mockStorage.Object);
        var key = "NonExistentKey";
        var defaultValue = "default-value";

        // Act
        var retrieved = profile.GetSetting(key, defaultValue);

        // Assert
        Assert.Equal(defaultValue, retrieved);
    }

    [Fact]
    public void SetSetting_OverwritesExistingValue()
    {
        // Arrange
        var profile = new Profile(_mockStorage.Object);
        var key = "TestKey";
        profile.SetSetting(key, "original");

        // Act
        profile.SetSetting(key, "modified");
        var retrieved = profile.GetSetting(key, "default");

        // Assert
        Assert.Equal("modified", retrieved);
    }

    [Fact]
    public void SetSetting_GetSetting_Int_RoundTrip()
    {
        // Arrange
        var profile = new Profile(_mockStorage.Object);
        var key = "TestInt";
        var value = 42;

        // Act
        profile.SetSetting(key, value);
        var retrieved = profile.GetSetting(key, 0);

        // Assert
        Assert.Equal(value, retrieved);
    }

    [Fact]
    public void SetSetting_GetSetting_EmptyList_RoundTrip()
    {
        // Arrange
        var profile = new Profile(_mockStorage.Object);
        var key = "TestEmptyList";
        var value = new List<string>();

        // Act
        profile.SetSetting(key, value);
        var retrieved = profile.GetSetting<List<string>>(key, new List<string> { "default" });

        // Assert
        Assert.NotNull(retrieved);
        Assert.Empty(retrieved);
    }

    [Fact]
    public void SetSetting_GetSetting_ListWithSpecialCharacters_RoundTrip()
    {
        // Arrange
        var profile = new Profile(_mockStorage.Object);
        var key = "TestSpecialChars";
        var value = new List<string> { "app.exe", "program files", "test&special", "path\\with\\backslashes" };

        // Act
        profile.SetSetting(key, value);
        var retrieved = profile.GetSetting<List<string>>(key, new List<string>());

        // Assert
        Assert.Equal(value, retrieved);
        Assert.Equal(4, retrieved.Count);
    }
}
