using LexFlow.Overlay.Interfaces;

namespace LexFlow.Overlay;

public sealed class ActionMenuOverlay : Win32LayeredPopup, IActionMenuOverlay
{
    private readonly object _lock = new();
    private List<string> _items = [];
    private string? _caption;
    private int _selected;
    private const int RowHeight = 38;
    private const int CaptionHeight = 28;

    public event EventHandler<ActionMenuItemEventArgs>? ItemSelected;
    public event EventHandler? Cancelled;

    public ActionMenuOverlay(OverlayThemeHost themeHost) : base("LexFlowActionMenuOverlay", "LexFlow Menu", themeHost)
    {
    }

    public bool IsVisible => Visible;

    public void ShowMenu(IReadOnlyList<string> items, int x, int y) => ShowMenu(items, x, y, null);

    public void ShowMenu(IReadOnlyList<string> items, int x, int y, string? caption)
    {
        lock (_lock)
        {
            _items = items.ToList();
            _caption = caption;
            _selected = 0;
        }

        var width = 420;
        var extra = string.IsNullOrEmpty(caption) ? 0 : CaptionHeight;
        var height = 20 + extra + Math.Max(1, items.Count) * RowHeight + 12;
        (x, y) = ClampToWorkArea(x, y, width, height);
        RequestShow(x, y, width, height);
    }

    public void SelectNext()
    {
        lock (_lock)
        {
            if (_items.Count == 0)
            {
                return;
            }

            _selected = (_selected + 1) % _items.Count;
        }

        Repaint();
    }

    public void SelectPrevious()
    {
        lock (_lock)
        {
            if (_items.Count == 0)
            {
                return;
            }

            _selected = (_selected - 1 + _items.Count) % _items.Count;
        }

        Repaint();
    }

    public void ConfirmSelection()
    {
        string text;
        int index;
        lock (_lock)
        {
            if (!IsShowing || _items.Count == 0)
            {
                return;
            }

            index = Math.Clamp(_selected, 0, _items.Count - 1);
            text = _items[index];
        }

        Hide();
        ItemSelected?.Invoke(this, new ActionMenuItemEventArgs { Index = index, Text = text });
    }

    public void Cancel()
    {
        if (!IsShowing)
        {
            return;
        }

        Hide();
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    protected override void Paint(Graphics g)
    {
        Chrome.Prepare(g);
        GetClientRect(WindowHandle, out var client);
        Chrome.DrawBorder(g, new Rectangle(0, 0, client.right - client.left, client.bottom - client.top));

        List<string> items;
        string? caption;
        int selected;
        lock (_lock)
        {
            items = _items.ToList();
            caption = _caption;
            selected = _selected;
        }

        using var font = new Font(OverlayChrome.FontName, 11);
        using var captionFont = new Font(OverlayChrome.FontName, 10);
        using var text = new SolidBrush(Chrome.Text);
        using var muted = new SolidBrush(Chrome.Muted);
        using var highlight = new SolidBrush(Chrome.Primary);
        var top = 10;
        if (!string.IsNullOrEmpty(caption))
        {
            g.DrawString(caption, captionFont, muted, 16, top);
            top += CaptionHeight;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var y = top + i * RowHeight;
            if (i == selected)
            {
                g.FillRectangle(highlight, 6, y, LastWidth - 12, RowHeight);
                g.DrawString(items[i], font, Brushes.White, 16, y + 8);
            }
            else
            {
                g.DrawString(items[i], font, text, 16, y + 8);
            }
        }
    }

    protected override void OnClick(IntPtr lParam)
    {
        var (_, y) = UnpackPoint(lParam);
        int extra;
        lock (_lock)
        {
            extra = string.IsNullOrEmpty(_caption) ? 0 : CaptionHeight;
        }

        var index = (y - 10 - extra) / RowHeight;
        lock (_lock)
        {
            if (index < 0 || index >= _items.Count)
            {
                return;
            }

            _selected = index;
        }

        ConfirmSelection();
    }
}
