using Microsoft.Win32;

namespace LexFlow.Settings;

/// <summary>
/// Manages Windows startup registration via registry Run key
/// </summary>
public static class WindowsStartup
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string AppName = "LexFlow";

    /// <summary>
    /// Gets whether the application is currently set to start with Windows
    /// </summary>
    public static bool IsEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            if (key == null) return false;

            var value = key.GetValue(AppName);
            return value != null;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Sets whether the application should start with Windows
    /// </summary>
    public static void SetEnabled(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
            if (key == null) return;

            if (enabled)
            {
                var exePath = Environment.ProcessPath ?? Environment.GetCommandLineArgs()[0];
                key.SetValue(AppName, $"\"{exePath}\"");
            }
            else
            {
                key.DeleteValue(AppName, false);
            }
        }
        catch
        {
            // Silently fail if registry access fails
        }
    }
}
