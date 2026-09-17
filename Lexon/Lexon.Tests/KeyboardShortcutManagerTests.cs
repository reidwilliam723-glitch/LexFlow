using Lexon.Input;
using Lexon.Input.Interfaces;
using Xunit;

namespace Lexon.Tests;

public class KeyboardShortcutManagerTests
{
    [Fact]
    public void RegisterShortcut_ValidShortcut_RegistersSuccessfully()
    {
        // Arrange
        var manager = new KeyboardShortcutManager();
        var shortcut = new KeyboardShortcut
        {
            Key = 0x41, // A
            Modifiers = KeyModifiers.Control
        };
        bool fired = false;
        EventHandler<KeyboardEventArgs>? handler = (s, e) => fired = true;

        // Act
        manager.RegisterShortcut("TestShortcut", shortcut, handler);

        // Assert
        Assert.True(fired == false); // Handler not called on registration
    }

    [Fact]
    public void UnregisterShortcut_ExistingShortcut_RemovesSuccessfully()
    {
        // Arrange
        var manager = new KeyboardShortcutManager();
        var shortcut = new KeyboardShortcut
        {
            Key = 0x41, // A
            Modifiers = KeyModifiers.Control
        };
        manager.RegisterShortcut("TestShortcut", shortcut, (s, e) => { });

        // Act
        manager.UnregisterShortcut("TestShortcut");

        // Assert - should not throw when trying to unregister again
        manager.UnregisterShortcut("TestShortcut");
    }

    [Fact]
    public void UnregisterShortcut_NonExistentShortcut_DoesNotThrow()
    {
        // Arrange
        var manager = new KeyboardShortcutManager();

        // Act & Assert
        manager.UnregisterShortcut("NonExistentShortcut");
    }

    [Fact]
    public void HandleKeyPress_MatchingShortcut_FiresHandler()
    {
        // Arrange
        var manager = new KeyboardShortcutManager();
        var shortcut = new KeyboardShortcut
        {
            Key = 0x41, // A
            Modifiers = KeyModifiers.Control
        };
        bool fired = false;
        manager.RegisterShortcut("TestShortcut", shortcut, (s, e) => fired = true);

        var args = new KeyboardEventArgs
        {
            VirtualKey = 0x41,
            IsControlPressed = true,
            IsShiftPressed = false,
            IsAltPressed = false
        };

        // Act
        manager.OnKeyPressed(args);

        // Assert
        Assert.True(fired);
    }

    [Fact]
    public void HandleKeyPress_PartialModifierMatch_DoesNotFire()
    {
        // Arrange
        var manager = new KeyboardShortcutManager();
        var shortcut = new KeyboardShortcut
        {
            Key = 0x41, // A
            Modifiers = KeyModifiers.Control | KeyModifiers.Shift
        };
        bool fired = false;
        manager.RegisterShortcut("TestShortcut", shortcut, (s, e) => fired = true);

        var args = new KeyboardEventArgs
        {
            VirtualKey = 0x41,
            IsControlPressed = true,
            IsShiftPressed = false, // Missing Shift
            IsAltPressed = false
        };

        // Act
        manager.OnKeyPressed(args);

        // Assert
        Assert.False(fired);
    }

    [Fact]
    public void HandleKeyPress_WrongKey_DoesNotFire()
    {
        // Arrange
        var manager = new KeyboardShortcutManager();
        var shortcut = new KeyboardShortcut
        {
            Key = 0x41, // A
            Modifiers = KeyModifiers.Control
        };
        bool fired = false;
        manager.RegisterShortcut("TestShortcut", shortcut, (s, e) => fired = true);

        var args = new KeyboardEventArgs
        {
            VirtualKey = 0x42, // B
            IsControlPressed = true,
            IsShiftPressed = false,
            IsAltPressed = false
        };

        // Act
        manager.OnKeyPressed(args);

        // Assert
        Assert.False(fired);
    }

    [Fact]
    public void HandleKeyPress_NoModifiers_DoesNotFireWhenModifiersRequired()
    {
        // Arrange
        var manager = new KeyboardShortcutManager();
        var shortcut = new KeyboardShortcut
        {
            Key = 0x41, // A
            Modifiers = KeyModifiers.Control
        };
        bool fired = false;
        manager.RegisterShortcut("TestShortcut", shortcut, (s, e) => fired = true);

        var args = new KeyboardEventArgs
        {
            VirtualKey = 0x41,
            IsControlPressed = false,
            IsShiftPressed = false,
            IsAltPressed = false
        };

        // Act
        manager.OnKeyPressed(args);

        // Assert
        Assert.False(fired);
    }

    [Fact]
    public void HandleKeyPress_MultipleShortcuts_FiresCorrectOne()
    {
        // Arrange
        var manager = new KeyboardShortcutManager();
        var shortcut1 = new KeyboardShortcut
        {
            Key = 0x41, // A
            Modifiers = KeyModifiers.Control
        };
        var shortcut2 = new KeyboardShortcut
        {
            Key = 0x42, // B
            Modifiers = KeyModifiers.Control
        };
        bool fired1 = false;
        bool fired2 = false;
        manager.RegisterShortcut("Shortcut1", shortcut1, (s, e) => fired1 = true);
        manager.RegisterShortcut("Shortcut2", shortcut2, (s, e) => fired2 = true);

        var args = new KeyboardEventArgs
        {
            VirtualKey = 0x41,
            IsControlPressed = true,
            IsShiftPressed = false,
            IsAltPressed = false
        };

        // Act
        manager.OnKeyPressed(args);

        // Assert
        Assert.True(fired1);
        Assert.False(fired2);
    }

    [Fact]
    public void OpenSettings_DefaultShortcut_IsCtrlShiftS()
    {
        var manager = new KeyboardShortcutManager();
        var shortcut = manager.GetShortcut("OpenSettings");

        Assert.NotNull(shortcut);
        Assert.Equal(0x53, shortcut!.Key);
        Assert.Equal(KeyModifiers.Control | KeyModifiers.Shift, shortcut.Modifiers);
        Assert.Equal("Ctrl + Shift + S", manager.GetShortcutDisplayText(shortcut));

        ShortcutTriggeredEventArgs? triggered = null;
        manager.ShortcutTriggered += (_, e) => triggered = e;

        var args = new KeyboardEventArgs
        {
            VirtualKey = 0x53,
            IsControlPressed = true,
            IsShiftPressed = true,
            IsAltPressed = false
        };
        manager.OnKeyPressed(args);

        Assert.NotNull(triggered);
        Assert.Equal("OpenSettings", triggered!.ShortcutName);
    }
}
