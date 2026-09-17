namespace Lexon.Onboarding.Wizard;

internal static class WizardLayout
{
    public const int ContentWidth = 750;

    public static FlowLayoutPanel CreateStack()
    {
        return new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Dock = DockStyle.Top,
            MaximumSize = new Size(ContentWidth, 0),
            Padding = Padding.Empty
        };
    }

    public static Label Title(string text, float size = 18f, FontStyle style = FontStyle.Bold, Color? foreColor = null, int bottomMargin = 24)
    {
        return new Label
        {
            Text = text,
            Font = new Font("Segoe UI", size, style),
            ForeColor = foreColor ?? SystemColors.ControlText,
            AutoSize = true,
            MaximumSize = new Size(ContentWidth, 0),
            Margin = new Padding(0, 0, 0, bottomMargin)
        };
    }

    public static Label Body(string text, Font? font = null, Color? foreColor = null, int bottomMargin = 16)
    {
        return new Label
        {
            Text = text,
            Font = font ?? new Font("Segoe UI", 12),
            ForeColor = foreColor ?? Color.FromArgb(60, 60, 60),
            AutoSize = true,
            MaximumSize = new Size(ContentWidth, 0),
            Margin = new Padding(0, 0, 0, bottomMargin)
        };
    }

    public static FlowLayoutPanel FeatureRow(CheckBox checkbox, string description)
    {
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            MaximumSize = new Size(ContentWidth, 0),
            Margin = new Padding(0, 0, 0, 12)
        };

        checkbox.AutoSize = true;
        checkbox.Margin = new Padding(0, 0, 0, 4);
        checkbox.Font = new Font("Segoe UI", 12);

        var desc = Body(description, new Font("Segoe UI", 11), Color.FromArgb(100, 100, 100), bottomMargin: 0);
        desc.Margin = new Padding(24, 0, 0, 0);

        row.Controls.Add(checkbox);
        row.Controls.Add(desc);
        return row;
    }

    public static FlowLayoutPanel FieldRow(string labelText, Control control)
    {
        var row = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            MaximumSize = new Size(ContentWidth, 0),
            Margin = new Padding(0, 0, 0, 20)
        };

        var label = new Label
        {
            Text = labelText,
            AutoSize = true,
            Font = new Font("Segoe UI", 12),
            Margin = new Padding(0, 0, 0, 6)
        };

        control.Margin = new Padding(0);
        control.MaximumSize = new Size(550, 0);

        row.Controls.Add(label);
        row.Controls.Add(control);
        return row;
    }
}
