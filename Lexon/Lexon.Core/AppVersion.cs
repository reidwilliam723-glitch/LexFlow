using System.Reflection;

namespace Lexon.Core;

/// <summary>
/// The running application's version, for anything that shows it to the person
/// using Lexon.
/// </summary>
public static class AppVersion
{
    /// <summary>
    /// Display form of the shipped version, e.g. "1.0.1".
    /// </summary>
    public static string Current { get; } = Read();

    private static string Read()
    {
        // The entry assembly is Lexon.Settings, which carries the <Version> that
        // release.ps1 packages. Reading the calling assembly instead would report
        // whichever library asked.
        var entry = Assembly.GetEntryAssembly();

        // Deliberately not GetName().Version: Directory.Build.props pins
        // AssemblyVersion to an unrelated value, so it does not track the shipped
        // release. The informational version does.
        var informational = entry
            ?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (string.IsNullOrWhiteSpace(informational))
        {
            return entry?.GetName().Version?.ToString(3) ?? "unknown";
        }

        // Builds with source revision info append "+<commit>". Keep just the
        // version: the full string is ~50 characters and NotifyIcon.Text throws
        // above 63.
        var plus = informational.IndexOf('+');
        return plus >= 0 ? informational[..plus] : informational;
    }
}
