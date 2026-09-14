using LexFlow.Core;

namespace LexFlow.Settings;

public sealed class CloudAiActivityForm : Form
{
    public CloudAiActivityForm(CloudAiActivityLog? log)
    {
        Text = "Cloud AI activity";
        Size = new Size(640, 420);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;

        var intro = new Label
        {
            Text = "Local only. Records when text was sent to a cloud provider — not the text itself.",
            Location = new Point(16, 12),
            Size = new Size(600, 36)
        };

        var list = new ListBox
        {
            Location = new Point(16, 52),
            Size = new Size(592, 280)
        };

        var entries = log?.Snapshot() ?? [];
        if (entries.Count == 0)
        {
            list.Items.Add("Nothing has been sent to a cloud AI provider yet.");
        }
        else
        {
            foreach (var entry in entries.AsEnumerable().Reverse())
            {
                list.Items.Add($"{entry.Utc:yyyy-MM-dd HH:mm:ss} UTC  {entry.Action}  {entry.Provider}  from {entry.ApplicationName}");
            }
        }

        var close = new Button { Text = "Close", DialogResult = DialogResult.OK, Location = new Point(528, 344), Size = new Size(80, 28) };
        Controls.Add(intro);
        Controls.Add(list);
        Controls.Add(close);
        AcceptButton = close;
    }
}
