using System.Runtime.InteropServices;
using System.Text;

namespace Lexon.Input;

internal readonly record struct FocusedTextInfo(string Text, int CaretIndex);

/// <summary>
/// Reads the focused editor via Win32 (WM_GETTEXT / EM_GETSEL).
/// The previous IAccessible COM layout was invalid and often returned the
/// control's Name (or empty text), which made CurrentWord wrong on accept.
/// </summary>
internal sealed class UIAutomationHelper
{
    private const int WmGetText = 0x000D;
    private const int WmGetTextLength = 0x000E;
    private const int EmGetSel = 0x00B0;

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    [DllImport("user32.dll")]
    private static extern bool GetGUIThreadInfo(uint idThread, ref GUITHREADINFO lpgui);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, StringBuilder lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode, EntryPoint = "SendMessageW")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, ref int wParam, ref int lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct GUITHREADINFO
    {
        public uint cbSize;
        public uint flags;
        public IntPtr hwndActive;
        public IntPtr hwndFocus;
        public IntPtr hwndCapture;
        public IntPtr hwndMenuOwner;
        public IntPtr hwndMoveSize;
        public IntPtr hwndCaret;
        public int rcCaretLeft;
        public int rcCaretTop;
        public int rcCaretRight;
        public int rcCaretBottom;
    }

    public FocusedTextInfo? TryReadSelection()
    {
        try
        {
            var hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
            {
                return null;
            }

            var focused = GetFocusedControl(hWnd);
            if (focused == IntPtr.Zero)
            {
                focused = hWnd;
            }

            var text = GetWindowTextContents(focused);
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var start = 0;
            var end = 0;
            SendMessage(focused, EmGetSel, ref start, ref end);
            if (start < 0 || end < 0 || start > text.Length || end > text.Length || start == end)
            {
                return null;
            }

            if (end < start)
            {
                (start, end) = (end, start);
            }

            return new FocusedTextInfo(text[start..end], start);
        }
        catch (Exception ex)
        {
            DiagnosticLog.Write($"UIAutomationHelper.TryReadSelection failed: {ex.Message}");
            return null;
        }
    }

    public FocusedTextInfo? TryReadFocusedText()
    {
        try
        {
            var hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
            {
                return null;
            }

            var focused = GetFocusedControl(hWnd);
            if (focused == IntPtr.Zero)
            {
                focused = hWnd;
            }

            var text = GetWindowTextContents(focused);
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            var caret = GetCaretIndex(focused, text.Length);
            return new FocusedTextInfo(text, caret);
        }
        catch (Exception ex)
        {
            DiagnosticLog.Write($"UIAutomationHelper.TryReadFocusedText failed: {ex.Message}");
            return null;
        }
    }

    private static IntPtr GetFocusedControl(IntPtr foregroundWindow)
    {
        var threadId = GetWindowThreadProcessId(foregroundWindow, out _);
        var guiInfo = new GUITHREADINFO { cbSize = (uint)Marshal.SizeOf<GUITHREADINFO>() };
        if (GetGUIThreadInfo(threadId, ref guiInfo) && guiInfo.hwndFocus != IntPtr.Zero)
        {
            return guiInfo.hwndFocus;
        }

        return IntPtr.Zero;
    }

    private static string GetWindowTextContents(IntPtr hWnd)
    {
        var length = SendMessage(hWnd, WmGetTextLength, IntPtr.Zero, IntPtr.Zero).ToInt32();
        if (length <= 0 || length > 1_000_000)
        {
            return string.Empty;
        }

        var builder = new StringBuilder(length + 1);
        SendMessage(hWnd, WmGetText, new IntPtr(builder.Capacity), builder);
        return builder.ToString();
    }

    private static int GetCaretIndex(IntPtr hWnd, int textLength)
    {
        var start = 0;
        var end = 0;
        SendMessage(hWnd, EmGetSel, ref start, ref end);
        if (start < 0 || start > textLength)
        {
            return textLength;
        }

        return start;
    }
}
