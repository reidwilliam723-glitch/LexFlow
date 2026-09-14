using System.IO;
using System.Text;

namespace LexFlow.Input;

/// <summary>
/// TEMPORARY diagnostic logger for tracking down the invisible-suggestion bug.
/// Writes to the same %LocalAppData%\LexFlow\diagnostic.log file as
/// LexFlow.Service's DiagnosticLog (duplicated here since LexFlow.Input
/// can't reference LexFlow.Service). Never throws. Remove once the bug is
/// found.
/// </summary>
internal static class DiagnosticLog
{
    internal static void Write(string message)
    {
        // High-frequency logging froze Settings save. No-op until needed.
    }

    internal static void WritePlacement(string message)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LexFlow");
            Directory.CreateDirectory(dir);
            File.AppendAllText(
                Path.Combine(dir, "placement.log"),
                $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
            // Logging must never throw.
        }
    }

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
