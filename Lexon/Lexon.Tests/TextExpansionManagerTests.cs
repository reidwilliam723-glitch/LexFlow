using Lexon.Core.Expansion;
using Lexon.Core.Interfaces;
using Lexon.Core.Models;
using Moq;
using Xunit;

namespace Lexon.Tests;

public class TextExpansionManagerTests
{
    private readonly Mock<IStorage> _mockStorage;

    public TextExpansionManagerTests()
    {
        _mockStorage = new Mock<IStorage>();
    }

    [Fact]
    public void CheckForTrigger_FiresAtStartOfInput()
    {
        // Arrange
        var manager = new TextExpansionManager(_mockStorage.Object);
        var expansion = new TextExpansion { Trigger = "sig", Expansion = "signature", IsEnabled = true };
        manager.AddExpansion(expansion);
        bool triggered = false;
        manager.ExpansionTriggered += (s, e) => triggered = true;

        // Act
        manager.OnCharacterTyped('s');
        manager.OnCharacterTyped('i');
        manager.OnCharacterTyped('g');

        // Assert
        Assert.True(triggered);
    }

    [Fact]
    public void CheckForTrigger_FiresAfterWhitespace()
    {
        // Arrange
        var manager = new TextExpansionManager(_mockStorage.Object);
        var expansion = new TextExpansion { Trigger = "sig", Expansion = "signature", IsEnabled = true };
        manager.AddExpansion(expansion);
        bool triggered = false;
        manager.ExpansionTriggered += (s, e) => triggered = true;

        // Act
        manager.OnCharacterTyped('h');
        manager.OnCharacterTyped('e');
        manager.OnCharacterTyped('l');
        manager.OnCharacterTyped('l');
        manager.OnCharacterTyped('o');
        manager.OnCharacterTyped(' ');
        manager.OnCharacterTyped('s');
        manager.OnCharacterTyped('i');
        manager.OnCharacterTyped('g');

        // Assert
        Assert.True(triggered);
    }

    [Fact]
    public void CheckForTrigger_FiresAfterPunctuation()
    {
        // Arrange
        var manager = new TextExpansionManager(_mockStorage.Object);
        var expansion = new TextExpansion { Trigger = "sig", Expansion = "signature", IsEnabled = true };
        manager.AddExpansion(expansion);
        bool triggered = false;
        manager.ExpansionTriggered += (s, e) => triggered = true;

        // Act
        manager.OnCharacterTyped('.');
        manager.OnCharacterTyped('s');
        manager.OnCharacterTyped('i');
        manager.OnCharacterTyped('g');

        // Assert
        Assert.True(triggered);
    }

    [Fact]
    public void CheckForTrigger_DoesNotFireMidWord()
    {
        // Arrange
        var manager = new TextExpansionManager(_mockStorage.Object);
        var expansion = new TextExpansion { Trigger = "sig", Expansion = "signature", IsEnabled = true };
        manager.AddExpansion(expansion);
        bool triggered = false;
        manager.ExpansionTriggered += (s, e) => triggered = true;

        // Act
        manager.OnCharacterTyped('d');
        manager.OnCharacterTyped('e');
        manager.OnCharacterTyped('s');
        manager.OnCharacterTyped('i');
        manager.OnCharacterTyped('g');
        manager.OnCharacterTyped('n');

        // Assert
        Assert.False(triggered);
    }

    [Fact]
    public void CheckForTrigger_DoesNotFireWhenTriggerIsSubstring()
    {
        // Arrange
        var manager = new TextExpansionManager(_mockStorage.Object);
        var expansion = new TextExpansion { Trigger = "sig", Expansion = "signature", IsEnabled = true };
        manager.AddExpansion(expansion);
        bool triggered = false;
        manager.ExpansionTriggered += (s, e) => triggered = true;

        // Act - type "design" which contains "sig" as a substring but not at the start
        manager.OnCharacterTyped('d');
        manager.OnCharacterTyped('e');
        manager.OnCharacterTyped('s');
        manager.OnCharacterTyped('i');
        manager.OnCharacterTyped('g');
        manager.OnCharacterTyped('n');

        // Assert - "sig" should not have triggered because it was part of "design" (not at word boundary)
        Assert.False(triggered);
    }

    [Fact]
    public void SetEnabled_DisablesExpansionWhenFalse()
    {
        // Arrange
        var manager = new TextExpansionManager(_mockStorage.Object);
        var expansion = new TextExpansion { Trigger = "sig", Expansion = "signature", IsEnabled = true };
        manager.AddExpansion(expansion);
        bool triggered = false;
        manager.ExpansionTriggered += (s, e) => triggered = true;

        // Act
        manager.SetEnabled(false);
        manager.OnCharacterTyped('s');
        manager.OnCharacterTyped('i');
        manager.OnCharacterTyped('g');

        // Assert
        Assert.False(triggered);
    }

    [Fact]
    public void SetEnabled_EnablesExpansionWhenTrue()
    {
        // Arrange
        var manager = new TextExpansionManager(_mockStorage.Object);
        var expansion = new TextExpansion { Trigger = "sig", Expansion = "signature", IsEnabled = true };
        manager.AddExpansion(expansion);
        bool triggered = false;
        manager.ExpansionTriggered += (s, e) => triggered = true;

        // Act
        manager.SetEnabled(false);
        manager.SetEnabled(true);
        manager.OnCharacterTyped('s');
        manager.OnCharacterTyped('i');
        manager.OnCharacterTyped('g');

        // Assert
        Assert.True(triggered);
    }
}
