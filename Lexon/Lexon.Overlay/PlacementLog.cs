namespace Lexon.Overlay;

internal static class PlacementLog
{
    internal static void Write(string message)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Lexon");
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
}
