using Lexon.Core.Theming;
using Xunit;

namespace Lexon.Tests;

public class ThemeNameTests
{
    [Theory]
    [InlineData("Light", true, "Light")]
    [InlineData("Dark", false, "Dark")]
    [InlineData("High Contrast", true, "High Contrast")]
    [InlineData("System", true, "Light")]
    [InlineData("System", false, "Dark")]
    [InlineData("", true, "Light")]
    [InlineData(null, false, "Light")]
    public void Resolve_MapsSystemAndPassthrough(string? name, bool systemIsLight, string expected)
    {
        Assert.Equal(expected, ThemeName.Resolve(name, systemIsLight));
    }
}
