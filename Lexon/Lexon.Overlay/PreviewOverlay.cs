using System.Drawing.Text;
using Lexon.Core;
using Lexon.Overlay.Interfaces;

namespace Lexon.Overlay;

public sealed class PreviewOverlay : Win32LayeredPopup, IPreviewOverlay
{
    private readonly object _lock = new();
    private readonly System.Threading.Timer _pulse;
    private string _before = string.Empty;
    private string _after = string.Empty;
    private bool _complete = true;
    private Rectangle _confirmRect;
    private Rectangle _cancelRect;

    public event EventHandler? Confirmed;
    public event EventHandler? Cancelled;

    public PreviewOverlay(OverlayThemeHost themeHost) : base("LexonPreviewOverlay", "Lexon Preview", themeHost)
    {
        _pulse = new System.Threading.Timer(_ =>
        {
            bool streaming;
            lock (_lock)
            {
                streaming = IsShowing && !_complete;
            }

            if (streaming)
            {
                Repaint();
            }
        }, null, Timeout.Infinite, Timeout.Infinite);
    }

    public bool IsVisible => Visible;

    public void ShowPreview(string before, string after, int x, int y)
        => ShowPreview(before, after, x, y, complete: true);

    public void ShowLivePreview(string before, int x, int y)
        => ShowPreview(before, string.Empty, x, y, complete: false);

    public void ShowPreview(string before, string after, int x, int y, bool complete)
    {
        lock (_lock)
        {
            _before = before ?? string.Empty;
            _after = after ?? string.Empty;
            _complete = complete;
        }

        const int width = 720;
        var height = Math.Clamp(340 + Math.Max(EditRisk.CountWords(before), 16) * 6, 380, 680);
        (x, y) = ClampToWorkArea(x + 16, y + 24, width, height);
        RequestShow(x, y, width, height);
        _pulse.Change(complete ? Timeout.Infinite : 50, complete ? Timeout.Infinite : 50);
    }

    public void UpdateAfter(string after, bool complete)
    {
        lock (_lock)
        {
            _after = after ?? string.Empty;
            _complete = complete;
        }

        _pulse.Change(complete ? Timeout.Infinite : 50, complete ? Timeout.Infinite : 50);
        Repaint();
    }

    public void Confirm()
    {
        lock (_lock)
        {
            if (!IsShowing || !_complete)
            {
                return;
            }
        }

        Hide();
        Confirmed?.Invoke(this, EventArgs.Empty);
    }

    public void Cancel()
    {
        if (!IsShowing)
        {
            return;
        }

        _pulse.Change(Timeout.Infinite, Timeout.Infinite);
        Hide();
        Cancelled?.Invoke(this, EventArgs.Empty);
    }

    public override void Hide()
    {
        _pulse.Change(Timeout.Infinite, Timeout.Infinite);
        base.Hide();
    }

    protected override void Paint(Graphics g)
    {
        Chrome.Prepare(g);
        GetClientRect(WindowHandle, out var client);
        var bounds = new Rectangle(0, 0, client.right - client.left, client.bottom - client.top);
        Chrome.DrawBorder(g, bounds);

        string before;
        string after;
        bool complete;
        lock (_lock)
        {
            before = _before;
            after = _after;
            complete = _complete;
        }

        using var titleFont = new Font(OverlayChrome.FontName, 12, FontStyle.Bold);
        using var bodyFont = new Font(OverlayChrome.FontName, 11);
        using var titleBrush = new SolidBrush(Chrome.Text);
        using var muted = new SolidBrush(Chrome.Muted);
        g.DrawString(complete ? "Review rewrite" : "Rewriting…", titleFont, titleBrush, 12, 10);
        g.DrawString(
            complete ? "Additions in green, removals in red." : "Text appears as it’s generated. Esc reverts.",
            bodyFont,
            muted,
            12,
            32);

        var barY = 52;
        DrawProgressBar(g, new Rectangle(12, barY, bounds.Width - 24, 6), complete);

        var diffBox = new Rectangle(12, 64, bounds.Width - 24, bounds.Height - 128);
        using (var fill = new SolidBrush(Chrome.PreviewInset))
        {
            g.FillRectangle(fill, diffBox);
        }

        if (complete)
        {
            DrawDiff(g, bodyFont, before, after, diffBox);
        }
        else
        {
            DrawStreamingText(g, bodyFont, after, diffBox);
        }

        _confirmRect = new Rectangle(bounds.Width - 220, bounds.Height - 50, 96, 34);
        _cancelRect = new Rectangle(bounds.Width - 112, bounds.Height - 50, 96, 34);
        Chrome.DrawButton(g, _confirmRect, "Apply", true);
        Chrome.DrawButton(g, _cancelRect, "Revert", false);
        g.DrawString(complete ? "Enter applies · Esc reverts" : "Esc reverts", bodyFont, muted, 12, bounds.Height - 42);
    }

    private void DrawProgressBar(Graphics g, Rectangle bar, bool complete)
    {
        using var track = new SolidBrush(Chrome.ProgressTrack);
        g.FillRectangle(track, bar);
        if (complete)
        {
            using var done = new SolidBrush(Chrome.Add);
            g.FillRectangle(done, bar);
            return;
        }

        var span = Math.Max(36, bar.Width / 4);
        var travel = Math.Max(1, bar.Width - span);
        var x = bar.X + (int)(Environment.TickCount64 / 8 % (travel * 2));
        if (x > travel)
        {
            x = travel * 2 - x;
        }

        using var fill = new SolidBrush(Chrome.Primary);
        g.FillRectangle(fill, new Rectangle(bar.X + x, bar.Y, span, bar.Height));
    }

    private void DrawStreamingText(Graphics g, Font font, string text, Rectangle box)
    {
        using var brush = new SolidBrush(Chrome.Text);
        var format = new StringFormat { Trimming = StringTrimming.EllipsisWord };
        g.DrawString(string.IsNullOrEmpty(text) ? "Waiting for the model…" : text, font, brush, box, format);
    }

    private void DrawDiff(Graphics g, Font font, string before, string after, Rectangle box)
    {
        var spans = TextDiff.Compare(before, after);
        float x = box.X + 8;
        float y = box.Y + 8;
        var maxX = box.Right - 8;
        var lineHeight = font.GetHeight(g) + 2;
        foreach (var span in spans)
        {
            Font? owned = null;
            var drawFont = font;
            if (span.Kind == DiffKind.Removed)
            {
                owned = new Font(font, FontStyle.Strikeout);
                drawFont = owned;
            }

            using var brush = new SolidBrush(span.Kind switch
            {
                DiffKind.Added => Chrome.Add,
                DiffKind.Removed => Chrome.Remove,
                _ => Chrome.Text
            });
            var remaining = span.Text;
            while (remaining.Length > 0)
            {
                if (y > box.Bottom - lineHeight)
                {
                    owned?.Dispose();
                    return;
                }

                var fit = remaining;
                while (fit.Length > 1 && x + g.MeasureString(fit, drawFont).Width > maxX && x > box.X + 8)
                {
                    x = box.X + 8;
                    y += lineHeight;
                    break;
                }

                while (fit.Length > 1 && x + g.MeasureString(fit, drawFont).Width > maxX)
                {
                    fit = fit[..^1];
                }

                g.DrawString(fit, drawFont, brush, x, y);
                x += g.MeasureString(fit, drawFont).Width;
                remaining = remaining[fit.Length..];
            }

            owned?.Dispose();
        }
    }

    protected override void OnClick(IntPtr lParam)
    {
        var (x, y) = UnpackPoint(lParam);
        if (_confirmRect.Contains(x, y))
        {
            Confirm();
        }
        else if (_cancelRect.Contains(x, y))
        {
            Cancel();
        }
    }

    public override void Dispose()
    {
        _pulse.Dispose();
        base.Dispose();
    }
}
