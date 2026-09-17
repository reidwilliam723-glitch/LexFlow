using System.Text;

namespace Lexon.Service;

/// <summary>
/// TEMPORARY diagnostic logger for tracking down the invisible-suggestion bug.
/// Writes to %LocalAppData%\Lexon\diagnostic.log. Never throws — logging
/// failures must never crash the app. Remove once the bug is found.
/// </summary>
internal static class DiagnosticLog
{
    internal static void Write(string message)
    {
        // High-frequency logging froze Settings save. No-op until needed.
    }

    /// <summary>
    /// Renders a string so hidden/invisible characters (spaces, control
    /// characters, etc.) are visible in the log instead of blending in.
    /// </summary>
    internal static string Escape(string? s)
    {
        if (s == null) return "<null>";
        if (s.Length == 0) return "<empty>";

        var sb = new StringBuilder();
        sb.Append('"');
        foreach (var c in s)
        {
            switch (c)
            {
                case ' ': sb.Append("[SP]"); break;
                case '\t': sb.Append("[TAB]"); break;
                case '\r': sb.Append("[CR]"); break;
                case '\n': sb.Append("[LF]"); break;
                default:
                    if (char.IsControl(c))
                    {
                        sb.Append($"[U+{(int)c:X4}]");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        sb.Append('"');
        sb.Append($" (len={s.Length})");
        return sb.ToString();
    }
}
