namespace Lexon.Input;

/// <summary>
/// Rules for overlay placement. Cursor/VS Code expose jumpy Chromium carets;
/// Chrome/Docs often report the tab/omnibox instead of the document.
/// </summary>
public static class CaretAnchorPolicy
{
    public static bool FreezeOverlayWhileWordContinues(string? processName)
        => WebEditorSupport.IsCodeEditorShell(processName);

    public static bool IsDummy(int x, int y)
        => (x == 100 && y == 100) || (x == 200 && y == 200) || (x == 0 && y == 0);

    public static bool IsLikelyBrowserChrome(int y, int windowTop, int contentTop)
        => contentTop > windowTop + 40 && y < contentTop;

    public static bool IsTeleport(int lastX, int lastY, int x, int y, int lineHeight)
    {
        if (lastX == 0 && lastY == 0)
        {
            return false;
        }

        var line = Math.Max(14, lineHeight);
        return Math.Abs(y - lastY) > line * 3 || Math.Abs(x - lastX) > 480;
    }

    public static bool IsTypingDrift(int lastX, int lastY, int x, int y, int lineHeight)
    {
        var line = Math.Max(14, lineHeight);
        return Math.Abs(y - lastY) <= line * 2
               && x >= lastX - line
               && x - lastX < 480;
    }

    public static int EstimateCharWidth(int lineHeight)
        => Math.Max(6, (int)Math.Round(Math.Clamp(lineHeight, 14, 48) * 0.52));
}
