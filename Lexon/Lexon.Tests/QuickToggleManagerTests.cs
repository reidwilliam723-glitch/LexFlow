using Lexon.Input;
using Lexon.Input.Interfaces;
using Xunit;

namespace Lexon.Tests;

public class QuickToggleManagerTests
{
    [Theory]
    [InlineData(0x11)]
    [InlineData(0xA2)]
    [InlineData(0xA3)]
    public void DoublePress_LeftOrRightCtrl_TogglesOff(int vk)
    {
        var manager = new QuickToggleManager();
        Assert.True(manager.IsEnabled);

        Press(manager, vk);
        Release(manager, vk);
        Press(manager, vk);

        Assert.False(manager.IsEnabled);
    }

    [Fact]
    public void HeldCtrl_DoesNotToggleFromKeyRepeat()
    {
        var manager = new QuickToggleManager();

        Press(manager, 0xA2);
        Press(manager, 0xA2);
        Press(manager, 0xA2);

        Assert.True(manager.IsEnabled);
    }

    [Fact]
    public void SinglePress_DoesNotToggle()
    {
        var manager = new QuickToggleManager();
        Press(manager, 0xA2);
        Release(manager, 0xA2);
        Assert.True(manager.IsEnabled);
    }

    private static void Press(QuickToggleManager manager, int vk)
    {
        manager.HandleKeyPress(new KeyboardEventArgs
        {
            VirtualKey = vk,
            IsControlPressed = true
        });
    }

    private static void Release(QuickToggleManager manager, int vk)
    {
        manager.HandleKeyRelease(new KeyboardEventArgs
        {
            VirtualKey = vk,
            IsControlPressed = false
        });
    }
}
