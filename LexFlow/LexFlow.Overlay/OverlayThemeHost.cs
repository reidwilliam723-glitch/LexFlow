using LexFlow.Core.Theming;

namespace LexFlow.Overlay;

/// <summary>
/// Subscribes to the core theme manager and exposes a shared overlay palette.
/// </summary>
public sealed class OverlayThemeHost : IDisposable
{
    private readonly ThemeManager _themeManager;
    private bool _disposed;

    public OverlayThemeHost(ThemeManager themeManager)
    {
        _themeManager = themeManager ?? throw new ArgumentNullException(nameof(themeManager));
        Palette = new OverlayThemePalette();
        Palette.Apply(_themeManager.CurrentTheme);
        _themeManager.ThemeChanged += OnThemeChanged;
    }

    public OverlayThemePalette Palette { get; }

    private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        Palette.Apply(e.NewTheme);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _themeManager.ThemeChanged -= OnThemeChanged;
        _disposed = true;
    }
}
