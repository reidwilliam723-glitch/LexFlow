using System.Drawing;
using Lexon.Core.Interfaces;
using Lexon.Core.Theming;
using Lexon.Overlay;
using Xunit;

namespace Lexon.Tests;

public class ThemeWiringTests
{
    [Fact]
    public void SetTheme_RaisesThemeChanged()
    {
        var storage = new MemoryStorage();
        var themeManager = new ThemeManager(storage);
        Theme? received = null;
        themeManager.ThemeChanged += (_, e) => received = e.NewTheme;

        themeManager.SetTheme("Dark");

        Assert.NotNull(received);
        Assert.Equal("Dark", received!.Name);
    }

    [Fact]
    public void OverlayThemePalette_ReadsCurrentThemeColors()
    {
        var palette = new OverlayThemePalette();
        var theme = new Theme
        {
            Name = "Test",
            Colors = new ThemeColors
            {
                Primary = "#0078D4",
                Secondary = "#106EBE",
                Background = "#101010",
                Surface = "#2D2D2D",
                Text = "#FFFFFF",
                TextSecondary = "#CCCCCC",
                Accent = "#8844AA",
                Success = "#107C10",
                Warning = "#FF8C00",
                Error = "#A80000",
                Border = "#505050"
            }
        };

        palette.Apply(theme);

        Assert.Equal(ColorTranslator.FromHtml("#2D2D2D"), palette.Background);
        Assert.Equal(ColorTranslator.FromHtml("#0078D4"), palette.SourceColor("Dictionary"));
        Assert.Equal(ColorTranslator.FromHtml("#8844AA"), palette.SourceColor("AI"));
    }

    private sealed class MemoryStorage : IStorage
    {
        private readonly Dictionary<string, string> _data = new(StringComparer.Ordinal);

        public Task SaveAsync<T>(string key, T data, CancellationToken cancellationToken = default)
        {
            _data[key] = System.Text.Json.JsonSerializer.Serialize(data);
            return Task.CompletedTask;
        }

        public Task<T?> LoadAsync<T>(string key, CancellationToken cancellationToken = default)
        {
            if (!_data.TryGetValue(key, out var json))
            {
                return Task.FromResult<T?>(default);
            }

            return Task.FromResult(System.Text.Json.JsonSerializer.Deserialize<T>(json));
        }

        public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
        {
            _data.Remove(key);
            return Task.CompletedTask;
        }

        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
            => Task.FromResult(_data.ContainsKey(key));
    }
}
