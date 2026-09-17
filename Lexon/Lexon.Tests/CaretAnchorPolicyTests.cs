using Lexon.Input;
using Xunit;

namespace Lexon.Tests;

public class CaretAnchorPolicyTests
{
    [Fact]
    public void FreezesOverlayInCursorNotChrome()
    {
        Assert.True(CaretAnchorPolicy.FreezeOverlayWhileWordContinues("Cursor.exe"));
        Assert.False(CaretAnchorPolicy.FreezeOverlayWhileWordContinues("chrome"));
    }

    [Fact]
    public void RejectsToolbarPointsAboveRenderWidget()
    {
        Assert.True(CaretAnchorPolicy.IsLikelyBrowserChrome(80, 40, 140));
        Assert.False(CaretAnchorPolicy.IsLikelyBrowserChrome(200, 40, 140));
    }

    [Fact]
    public void DetectsCaretTeleportVersusTyping()
    {
        Assert.True(CaretAnchorPolicy.IsTeleport(400, 400, 80, 80, 20));
        Assert.True(CaretAnchorPolicy.IsTypingDrift(400, 400, 412, 400, 20));
        Assert.False(CaretAnchorPolicy.IsTypingDrift(400, 400, 80, 80, 20));
    }
}
