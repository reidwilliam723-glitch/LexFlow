using System.Runtime.InteropServices;

namespace Lexon.Input;

/// <summary>
/// Reads OBJID_CARET via MSAA. Chromium sometimes exposes a real caret
/// here even when GUITHREADINFO is empty or points at the omnibox.
/// </summary>
internal static class AccessibleCaret
{
    private const uint ObjIdCaret = 0xFFFFFFF8;
    private static readonly Guid IidIAccessible = new("618736E0-3C3D-11CF-810C-00AA00389B71");

    [DllImport("oleacc.dll")]
    private static extern int AccessibleObjectFromWindow(
        IntPtr hwnd,
        uint dwId,
        ref Guid riid,
        [MarshalAs(UnmanagedType.IUnknown)] out object? ppvObject);

    public static bool TryGetTopLeft(IntPtr hwnd, out int x, out int y, out int height)
    {
        x = 0;
        y = 0;
        height = 20;
        if (hwnd == IntPtr.Zero)
        {
            return false;
        }

        object? unk = null;
        try
        {
            var iid = IidIAccessible;
            if (AccessibleObjectFromWindow(hwnd, ObjIdCaret, ref iid, out unk) != 0 || unk == null)
            {
                return false;
            }

            dynamic acc = unk;
            int left = 0, top = 0, width = 0, bottom = 0;
            acc.accLocation(out left, out top, out width, out bottom, 0);
            if (bottom is < 8 or > 72)
            {
                return false;
            }

            x = left;
            y = top;
            height = Math.Max(14, bottom);
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if (unk != null)
            {
                Marshal.FinalReleaseComObject(unk);
            }
        }
    }
}
