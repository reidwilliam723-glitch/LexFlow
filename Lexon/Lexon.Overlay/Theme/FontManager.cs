using System.Drawing;

namespace Lexon.Overlay.Theme;

/// <summary>
/// Adjustable font size system
/// </summary>
public class FontManager
{
    private FontSize _currentFontSize = FontSize.Medium;
    private readonly Dictionary<FontSize, Font> _fontCache = new();
    private string _fontFamily = "Segoe UI";

    public FontSize CurrentFontSize
    {
        get => _currentFontSize;
        set
        {
            _currentFontSize = value;
            FontSizeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public string FontFamily
    {
        get => _fontFamily;
        set
        {
            _fontFamily = value;
            _fontCache.Clear();
            FontSizeChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public event EventHandler? FontSizeChanged;

    public FontManager()
    {
        InitializeFontCache();
    }

    private void InitializeFontCache()
    {
        _fontCache[FontSize.Small] = new Font(_fontFamily, 8);
        _fontCache[FontSize.Medium] = new Font(_fontFamily, 9);
        _fontCache[FontSize.Large] = new Font(_fontFamily, 11);
        _fontCache[FontSize.ExtraLarge] = new Font(_fontFamily, 13);
        _fontCache[FontSize.ExtraExtraLarge] = new Font(_fontFamily, 15);
    }

    public Font GetFont(FontSize size)
    {
        if (!_fontCache.ContainsKey(size))
        {
            var fontSize = size switch
            {
                FontSize.Small => 8,
                FontSize.Medium => 9,
                FontSize.Large => 11,
                FontSize.ExtraLarge => 13,
                FontSize.ExtraExtraLarge => 15,
                _ => 9
            };
            _fontCache[size] = new Font(_fontFamily, fontSize);
        }

        return _fontCache[size];
    }

    public Font GetCurrentFont()
    {
        return GetFont(_currentFontSize);
    }

    public void IncreaseFontSize()
    {
        var sizes = Enum.GetValues<FontSize>();
        var currentIndex = Array.IndexOf(sizes, _currentFontSize);
        
        if (currentIndex < sizes.Length - 1)
        {
            CurrentFontSize = sizes[currentIndex + 1];
        }
    }

    public void DecreaseFontSize()
    {
        var sizes = Enum.GetValues<FontSize>();
        var currentIndex = Array.IndexOf(sizes, _currentFontSize);
        
        if (currentIndex > 0)
        {
            CurrentFontSize = sizes[currentIndex - 1];
        }
    }

    public void SetCustomFontSize(float size)
    {
        _fontCache[FontSize.Custom] = new Font(_fontFamily, size);
        CurrentFontSize = FontSize.Custom;
    }

    public void ApplyToControl(Control control, FontSize? size = null)
    {
        var font = size.HasValue ? GetFont(size.Value) : GetCurrentFont();
        control.Font = font;
    }

    public void ApplyToControls(IEnumerable<Control> controls, FontSize? size = null)
    {
        var font = size.HasValue ? GetFont(size.Value) : GetCurrentFont();
        foreach (var control in controls)
        {
            control.Font = font;
        }
    }

    public float GetPixelSize(FontSize size)
    {
        return size switch
        {
            FontSize.Small => 8,
            FontSize.Medium => 9,
            FontSize.Large => 11,
            FontSize.ExtraLarge => 13,
            FontSize.ExtraExtraLarge => 15,
            FontSize.Custom => _fontCache.ContainsKey(FontSize.Custom) ? _fontCache[FontSize.Custom].Size : 9,
            _ => 9
        };
    }

    public void ResetToDefault()
    {
        CurrentFontSize = FontSize.Medium;
        _fontFamily = "Segoe UI";
        _fontCache.Clear();
        InitializeFontCache();
    }
}

public enum FontSize
{
    Small,
    Medium,
    Large,
    ExtraLarge,
    ExtraExtraLarge,
    Custom
}
