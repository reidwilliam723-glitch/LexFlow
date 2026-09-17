using Xunit;

namespace Lexon.Tests;

public class ThemeUiComboTests
{
    [Fact]
    public void ComboGetItemText_UsesDisplayMemberName()
    {
        using var combo = new ComboBox { DisplayMember = "Name" };
        combo.Items.Add(new NamedItem { Name = "Writer", Id = "writer" });

        Assert.Equal("Writer", combo.GetItemText(combo.Items[0]));
        Assert.NotEqual("Writer", combo.Items[0]!.ToString());
    }

    private sealed class NamedItem
    {
        public string Name { get; init; } = string.Empty;
        public string Id { get; init; } = string.Empty;
    }
}
