using LexFlow.Input;
using LexFlow.Input.Interfaces;
using Moq;
using Xunit;

namespace LexFlow.Tests;

public class UndoManagerTests
{
    private readonly Mock<ITextInjector> _mockInjector;

    public UndoManagerTests()
    {
        _mockInjector = new Mock<ITextInjector>();
    }

    [Fact]
    public void RecordOperation_ThenUndo_RestoresOriginalText()
    {
        // Arrange
        var manager = new UndoManager(_mockInjector.Object);
        var originalText = "original";
        var newText = "modified";

        // Act
        manager.RecordOperation(originalText, newText);
        manager.Undo();

        // Assert
        _mockInjector.Verify(x => x.DeleteBackward(newText.Length), Times.Once);
        _mockInjector.Verify(x => x.InjectText(originalText), Times.Once);
    }

    [Fact]
    public void RecordOperation_MultipleOperations_UndoesInReverseOrder()
    {
        // Arrange
        var manager = new UndoManager(_mockInjector.Object);
        manager.RecordOperation("a", "b");
        manager.RecordOperation("b", "c");
        manager.RecordOperation("c", "d");

        // Act
        manager.Undo(); // Should undo c->d, restoring "c"

        // Assert
        _mockInjector.Verify(x => x.DeleteBackward(1), Times.Once); // Delete "d"
        _mockInjector.Verify(x => x.InjectText("c"), Times.Once);
    }

    [Fact]
    public void RecordOperation_ExceedsMaxStackSize_KeepsMostRecent()
    {
        // Arrange
        var manager = new UndoManager(_mockInjector.Object);
        
        // Record more operations than MaxStackSize (which is 50)
        for (int i = 0; i < 60; i++)
        {
            manager.RecordOperation($"text{i}", $"text{i + 1}");
        }

        // Act
        manager.Undo(); // Should undo text59->text60, restoring "text59"

        // Assert
        _mockInjector.Verify(x => x.DeleteBackward(It.IsAny<int>()), Times.Once);
        _mockInjector.Verify(x => x.InjectText("text59"), Times.Once);
    }

    [Fact]
    public void Undo_WhenStackEmpty_DoesNothing()
    {
        // Arrange
        var manager = new UndoManager(_mockInjector.Object);

        // Act
        manager.Undo();

        // Assert
        _mockInjector.Verify(x => x.DeleteBackward(It.IsAny<int>()), Times.Never);
        _mockInjector.Verify(x => x.InjectText(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Redo_AfterUndo_RestoresUndoneChange()
    {
        // Arrange
        var manager = new UndoManager(_mockInjector.Object);
        manager.RecordOperation("original", "modified");
        manager.Undo();

        // Act
        manager.Redo();

        // Assert
        _mockInjector.Verify(x => x.DeleteBackward(It.IsAny<int>()), Times.Exactly(2)); // Once in Undo, once in Redo
        _mockInjector.Verify(x => x.InjectText("modified"), Times.Once);
    }

    [Fact]
    public void Redo_WhenNoUndoAvailable_DoesNothing()
    {
        // Arrange
        var manager = new UndoManager(_mockInjector.Object);

        // Act
        manager.Redo();

        // Assert
        _mockInjector.Verify(x => x.DeleteBackward(It.IsAny<int>()), Times.Never);
        _mockInjector.Verify(x => x.InjectText(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void RecordOperation_AfterUndo_ClearsRedoStack()
    {
        // Arrange
        var manager = new UndoManager(_mockInjector.Object);
        manager.RecordOperation("a", "b");
        manager.RecordOperation("b", "c");
        manager.Undo(); // Undo b->c

        // Act
        manager.RecordOperation("b", "d"); // New operation
        manager.Redo(); // Should not redo b->c

        // Assert
        _mockInjector.Verify(x => x.InjectText("c"), Times.Never);
    }

    [Fact]
    public void CanUndo_ReturnsTrueWhenOperationsAvailable()
    {
        // Arrange
        var manager = new UndoManager(_mockInjector.Object);
        manager.RecordOperation("a", "b");

        // Act
        var canUndo = manager.CanUndo;

        // Assert
        Assert.True(canUndo);
    }

    [Fact]
    public void CanUndo_ReturnsFalseWhenStackEmpty()
    {
        // Arrange
        var manager = new UndoManager(_mockInjector.Object);

        // Act
        var canUndo = manager.CanUndo;

        // Assert
        Assert.False(canUndo);
    }

    [Fact]
    public void CanRedo_ReturnsTrueAfterUndo()
    {
        // Arrange
        var manager = new UndoManager(_mockInjector.Object);
        manager.RecordOperation("a", "b");
        manager.Undo();

        // Act
        var canRedo = manager.CanRedo;

        // Assert
        Assert.True(canRedo);
    }

    [Fact]
    public void CanRedo_ReturnsFalseWhenNoUndo()
    {
        // Arrange
        var manager = new UndoManager(_mockInjector.Object);

        // Act
        var canRedo = manager.CanRedo;

        // Assert
        Assert.False(canRedo);
    }
}
