namespace Lexon.Core.Theming;

/// <summary>
/// Maps UI theme choices onto the built-in theme names ThemeManager understands.
/// "System" follows the host light/dark setting supplied by the caller.
/// </summary>
public static class ThemeName
{
    public static string Resolve(string? name, bool systemIsLight = true)
    {
        if (string.Equals(name, "System", StringComparison.OrdinalIgnoreCase))
        {
            return systemIsLight ? "Light" : "Dark";
        }

        return string.IsNullOrWhiteSpace(name) ? "Light" : name;
    }
}
