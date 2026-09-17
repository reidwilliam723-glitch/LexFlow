namespace Lexon.Overlay;

public sealed class SelectionChipOverlay : Win32LayeredPopup
{
    public event EventHandler? Clicked;

    public SelectionChipOverlay(OverlayThemeHost themeHost) : base("LexonSelectionChipOverlay", "Lexon Chip", themeHost)
    {
    }

    public bool IsVisible => Visible;

    public void ShowNear(int x, int y)
    {
        const int size = 28;
        (x, y) = ClampToWorkArea(x + 8, y - size - 6, size, size);
        RequestShow(x, y, size, size);
    }

    protected override void Paint(Graphics g)
    {
        Chrome.Prepare(g);
        GetClientRect(WindowHandle, out var client);
        var bounds = new Rectangle(1, 1, client.right - client.left - 2, client.bottom - client.top - 2);
        using var path = OverlayChrome.Rounded(bounds, OverlayChrome.CornerRadius);
        using var fill = new SolidBrush(Chrome.Primary);
        g.FillPath(fill, path);
        using var font = new Font(OverlayChrome.FontName, 9, FontStyle.Bold);
        using var brush = new SolidBrush(Color.White);
        g.DrawString("Aa", font, brush, 3, 5);
    }

    protected override void OnClick(IntPtr lParam)
    {
        Hide();
        Clicked?.Invoke(this, EventArgs.Empty);
    }
}
