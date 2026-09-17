using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Lexon.Service;

/// <summary>
/// Short-lived, non-activating toast used for enable/disable feedback.
/// </summary>
internal sealed class StatusToastForm : Form
{
    private string _message = string.Empty;
    private Color _accent = Color.FromArgb(80, 200, 120);

    public StatusToastForm()
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        TopMost = true;
        StartPosition = FormStartPosition.Manual;
        Size = new Size(320, 56);
        BackColor = Color.FromArgb(32, 32, 36);
        DoubleBuffered = true;
        Padding = new Padding(16, 12, 16, 12);
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WsExNoActivate = 0x08000000;
            const int WsExToolWindow = 0x00000080;
            const int WsExTopMost = 0x00000008;

            var cp = base.CreateParams;
            cp.ExStyle |= WsExNoActivate | WsExToolWindow | WsExTopMost;
            return cp;
        }
    }

    public void ShowMessage(string message, bool enabled)
    {
        _message = message;
        _accent = enabled
            ? Color.FromArgb(80, 200, 120)
            : Color.FromArgb(200, 90, 90);

        using var font = CreateMessageFont();
        var textSize = TextRenderer.MeasureText(
            message,
            font,
            new Size(int.MaxValue, int.MaxValue),
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        Width = Math.Max(300, textSize.Width + 48);
        Height = Math.Max(56, textSize.Height + 28);

        Invalidate();

        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(
            area.Left + (area.Width - Width) / 2,
            area.Bottom - Height - 48);

        if (!Visible)
        {
            Show();
        }

        BringToFront();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(BackColor);

        using (var accent = new SolidBrush(_accent))
        {
            g.FillRectangle(accent, 0, 8, 4, Height - 16);
        }

        using var font = CreateMessageFont();
        var bounds = new Rectangle(18, 0, Width - 28, Height);
        TextRenderer.DrawText(
            g,
            _message,
            font,
            bounds,
            Color.FromArgb(240, 240, 244),
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPadding);
    }

    private static Font CreateMessageFont() => new("Segoe UI", 10.5f, FontStyle.Regular);
}
