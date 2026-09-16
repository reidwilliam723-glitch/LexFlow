using System.Reflection;
using System.Runtime.InteropServices;
using LexFlow.Core.Theming;

namespace LexFlow.Settings;

internal static class ThemeUi
{
    private const int WM_SETREDRAW = 0x000B;

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, bool wParam, int lParam);

    public static Color Background(Theme theme) => ColorTranslator.FromHtml(theme.Colors.Background);

    public static Color Foreground(Theme theme) => ColorTranslator.FromHtml(theme.Colors.Text);

    public static Color Surface(Theme theme) => ColorTranslator.FromHtml(theme.Colors.Surface);

    public static Color Primary(Theme theme) => ColorTranslator.FromHtml(theme.Colors.Primary);

    public static Color InputBack(Theme theme)
    {
        var background = Background(theme);
        var luminance = (0.299 * background.R) + (0.587 * background.G) + (0.114 * background.B);
        return luminance < 140 ? Surface(theme) : Color.White;
    }

    public static void EnableBufferedPaint(Control control)
    {
        typeof(Control).InvokeMember(
            "DoubleBuffered",
            BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic,
            binder: null,
            target: control,
            args: new object[] { true });
    }

    public static void AttachComboDrawing(ComboBox combo)
    {
        combo.FlatStyle = FlatStyle.Flat;
        combo.DrawMode = DrawMode.OwnerDrawFixed;
        combo.DrawItem -= DrawComboItem;
        combo.DrawItem += DrawComboItem;
    }

    public static void ApplyToTree(Control root, Theme theme)
    {
        ApplyToControl(root, theme);
    }

    /// <summary>
    /// Recolours a whole window in a single repaint. Every colour assignment during
    /// the tree walk queues its own paint, which on a form with this many controls
    /// shows up as a visible top-to-bottom wave. SuspendLayout alone does not help,
    /// because it batches layout rather than painting, so drawing is switched off at
    /// the window level for the duration of the walk.
    /// </summary>
    public static void ApplyToTreeWithoutFlicker(Form form, Theme theme)
    {
        // Nothing to suspend before the window exists, and reading Handle here would
        // force it to be created early, which callers theming during construction do
        // not expect.
        if (!form.IsHandleCreated)
        {
            ApplyToTree(form, theme);
            return;
        }

        SendMessage(form.Handle, WM_SETREDRAW, false, 0);
        try
        {
            form.SuspendLayout();
            ApplyToTree(form, theme);
            form.ResumeLayout(true);
        }
        finally
        {
            // Must run even if the walk throws, or the window stays permanently
            // frozen with drawing disabled.
            SendMessage(form.Handle, WM_SETREDRAW, true, 0);
        }

        form.Invalidate(true);
        form.Refresh();
    }

    public static void StyleComboBox(ComboBox combo, Theme theme)
    {
        combo.BackColor = InputBack(theme);
        combo.ForeColor = Foreground(theme);
    }

    private static void ApplyToControl(Control control, Theme theme)
    {
        switch (control)
        {
            case CheckBox check:
                check.UseVisualStyleBackColor = false;
                check.BackColor = Background(theme);
                check.ForeColor = Foreground(theme);
                return;
            case Button button:
                button.BackColor = Primary(theme);
                button.ForeColor = Color.White;
                button.FlatStyle = FlatStyle.Flat;
                button.FlatAppearance.BorderSize = 0;
                return;
            case ComboBox combo:
                StyleComboBox(combo, theme);
                return;
            case TextBox or ListBox or ListView:
                control.BackColor = InputBack(theme);
                control.ForeColor = Foreground(theme);
                return;
            default:
                control.BackColor = Background(theme);
                control.ForeColor = Foreground(theme);
                break;
        }

        foreach (Control child in control.Controls)
        {
            ApplyToControl(child, theme);
        }
    }

    private static void DrawComboItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox combo)
        {
            return;
        }

        var index = e.Index >= 0 ? e.Index : combo.SelectedIndex;
        var selected = (e.State & DrawItemState.Selected) != 0;
        var back = selected ? SystemColors.Highlight : combo.BackColor;
        var fore = selected ? SystemColors.HighlightText : combo.ForeColor;

        using var fill = new SolidBrush(back);
        e.Graphics.FillRectangle(fill, e.Bounds);

        if (index < 0)
        {
            return;
        }

        TextRenderer.DrawText(
            e.Graphics,
            combo.Items[index]?.ToString() ?? string.Empty,
            combo.Font,
            e.Bounds,
            fore,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter);
    }
}
