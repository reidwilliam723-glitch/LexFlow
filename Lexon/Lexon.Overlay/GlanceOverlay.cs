namespace Lexon.Overlay;

public sealed class GlanceOverlay : Win32LayeredPopup
{
    private string _message = string.Empty;
    private readonly System.Threading.Timer _hideTimer;

    public GlanceOverlay(OverlayThemeHost themeHost) : base("LexonGlanceOverlay", "Lexon Glance", themeHost)
    {
        _hideTimer = new System.Threading.Timer(_ => Hide(), null, Timeout.Infinite, Timeout.Infinite);
    }

    public void ShowGlance(string message, int x, int y)
    {
        _message = message ?? string.Empty;
        const int width = 340;
        const int height = 44;
        (x, y) = ClampToWorkArea(x + 12, y + 20, width, height);
        RequestShow(x, y, width, height);
        _hideTimer.Change(2500, Timeout.Infinite);
    }

    protected override void Paint(Graphics g)
    {
        Chrome.Prepare(g);
        GetClientRect(WindowHandle, out var client);
        var bounds = new Rectangle(0, 0, client.right - client.left, client.bottom - client.top);
        Chrome.DrawBorder(g, bounds);
        using var font = new Font(OverlayChrome.FontName, 9);
        using var brush = new SolidBrush(Chrome.Text);
        g.DrawString(_message, font, brush, new RectangleF(12, 12, bounds.Width - 24, bounds.Height - 20));
    }

    public override void Dispose()
    {
        _hideTimer.Dispose();
        base.Dispose();
    }
}
