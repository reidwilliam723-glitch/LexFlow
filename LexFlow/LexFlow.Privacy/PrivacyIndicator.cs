using System.Windows.Forms;
using System.ComponentModel;

namespace LexFlow.Privacy;

/// <summary>
/// Visual indicator for privacy mode status
/// </summary>
public class PrivacyIndicator : Form
{
    private PrivacyStatus _currentStatus = PrivacyStatus.Active;
    private Label _statusLabel = null!;
    private Label _detailsLabel = null!;
    private System.Windows.Forms.Timer _updateTimer = null!;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public PrivacyStatus CurrentStatus
    {
        get => _currentStatus;
        set
        {
            _currentStatus = value;
            UpdateDisplay();
        }
    }

    public PrivacyIndicator()
    {
        InitializeComponent();
        InitializeTimer();
    }

    private void InitializeComponent()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.TopMost = true;
        this.ShowInTaskbar = false;
        this.BackColor = Color.FromArgb(45, 45, 48);
        this.Size = new Size(250, 60);
        this.StartPosition = FormStartPosition.Manual;

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _statusLabel = new Label
        {
            Font = new Font("Segoe UI", 9, FontStyle.Bold),
            Location = new Point(10, 10),
            AutoSize = true,
            ForeColor = Color.White
        };

        _detailsLabel = new Label
        {
            Font = new Font("Segoe UI", 8),
            Location = new Point(10, 30),
            Size = new Size(230, 20),
            ForeColor = Color.FromArgb(200, 200, 200)
        };

        panel.Controls.Add(_statusLabel);
        panel.Controls.Add(_detailsLabel);
        this.Controls.Add(panel);

        UpdateDisplay();
    }

    private void InitializeTimer()
    {
        _updateTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _updateTimer.Tick += (s, e) => UpdateDisplay();
        _updateTimer.Start();
    }

    private void UpdateDisplay()
    {
        var (statusText, detailsText, color) = _currentStatus switch
        {
            PrivacyStatus.Active => ("Privacy Mode: ON", "All data processed locally", Color.FromArgb(16, 185, 129)),
            PrivacyStatus.Inactive => ("Privacy Mode: OFF", "Cloud features enabled", Color.FromArgb(245, 159, 11)),
            PrivacyStatus.SecureField => ("Secure Field Detected", "Suggestions blocked", Color.FromArgb(217, 83, 79)),
            PrivacyStatus.LocalOnly => ("Local Only Mode", "No cloud features", Color.FromArgb(0, 120, 215)),
            _ => ("Privacy Mode: Unknown", "Status unavailable", Color.Gray)
        };

        _statusLabel.Text = statusText;
        _statusLabel.ForeColor = color;
        _detailsLabel.Text = detailsText;
    }

    public void ShowAt(int x, int y)
    {
        this.Location = new Point(x, y - 70);
        this.Show();
    }

    public void HideAfter(TimeSpan delay)
    {
        Task.Delay(delay).ContinueWith(_ => this.Hide());
    }

    protected override bool ShowWithoutActivation
    {
        get { return true; }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_NOACTIVATE = 0x08000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_NOACTIVATE;
            return cp;
        }
    }
}

public enum PrivacyStatus
{
    Active,
    Inactive,
    SecureField,
    LocalOnly
}
