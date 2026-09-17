using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Lexon.Ui;

/// <summary>
/// Stops combo boxes from eating page-scroll gestures. Subclassing each ComboBox
/// is unreliable (handles get recreated, and Windows often routes the wheel to the
/// focused box rather than the one under the cursor). A form-level message filter
/// intercepts the wheel before any combo sees it and hands it to the scrolling
/// panel instead.
/// </summary>
public static class ComboWheel
{
    private const int WM_MOUSEWHEEL = 0x020A;

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(POINT point);

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    private static readonly ConditionalWeakTable<Form, WheelFilter> Filters = new();

    public static void Guard(ComboBox combo)
    {
        ArgumentNullException.ThrowIfNull(combo);
        Install(combo);
    }

    public static void GuardTree(Control root)
    {
        ArgumentNullException.ThrowIfNull(root);
        Install(root);
    }

    private static void Install(Control control)
    {
        void Attach(object? sender, EventArgs e)
        {
            control.HandleCreated -= Attach;
            if (control.FindForm() is { } form)
            {
                EnsureFilter(form);
            }
        }

        var form = control.FindForm();
        if (form != null)
        {
            EnsureFilter(form);
            return;
        }

        control.HandleCreated += Attach;
        if (control is Form asForm)
        {
            EnsureFilter(asForm);
        }
    }

    private static void EnsureFilter(Form form)
    {
        if (!Filters.TryGetValue(form, out _))
        {
            Filters.Add(form, new WheelFilter(form));
        }
    }

    private sealed class WheelFilter : IMessageFilter
    {
        private readonly Form _form;

        public WheelFilter(Form form)
        {
            _form = form;
            Application.AddMessageFilter(this);
            form.FormClosed += (_, _) => Application.RemoveMessageFilter(this);
        }

        public bool PreFilterMessage(ref Message m)
        {
            if (m.Msg != WM_MOUSEWHEEL || _form.IsDisposed || !_form.Visible)
            {
                return false;
            }

            // An open dropdown is the one case where the wheel belongs to the combo.
            if (AnyDroppedDown(_form))
            {
                return false;
            }

            var screen = Cursor.Position;
            var hit = Control.FromHandle(WindowFromPoint(new POINT { X = screen.X, Y = screen.Y }));
            if (hit == null || hit.FindForm() != _form)
            {
                return false;
            }

            var scrollable = FindScrollableAncestor(hit);
            if (scrollable is not { IsHandleCreated: true })
            {
                return false;
            }

            SendMessage(scrollable.Handle, m.Msg, m.WParam, m.LParam);
            return true;
        }

        private static bool AnyDroppedDown(Control root)
        {
            if (root is ComboBox { DroppedDown: true })
            {
                return true;
            }

            foreach (Control child in root.Controls)
            {
                if (AnyDroppedDown(child))
                {
                    return true;
                }
            }

            return false;
        }

        private static Control? FindScrollableAncestor(Control? control)
        {
            while (control != null && control is not ScrollableControl { AutoScroll: true })
            {
                control = control.Parent;
            }

            return control;
        }
    }
}
