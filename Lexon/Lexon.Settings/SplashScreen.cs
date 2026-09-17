using System.Drawing;
using System.Windows.Forms;

namespace Lexon.Settings;

/// <summary>
/// Professional splash screen for Lexon application
/// </summary>
public class SplashScreen : Form
{
    private ProgressBar _progressBar;
    private Label _statusLabel;
    private Label _versionLabel;
    private PictureBox _logoPictureBox;
    private int _loadingProgress = 0;

    public SplashScreen()
    {
        // Form setup
        this.Text = "Lexon";
        this.FormBorderStyle = FormBorderStyle.None;
        this.StartPosition = FormStartPosition.CenterScreen;
        this.Size = new Size(500, 300);
        this.BackColor = Color.White;
        this.ShowInTaskbar = false;
        this.TopMost = true;

        InitializeComponents();
    }

    private void InitializeComponents()
    {
        // Logo area
        _logoPictureBox = new PictureBox
        {
            Size = new Size(120, 120),
            Location = new Point(190, 40),
            BackColor = Color.FromArgb(30, 136, 229), // Blue background
            SizeMode = PictureBoxSizeMode.CenterImage
        };

        // Add "L" text as logo
        var logoText = new Label
        {
            Text = "L",
            Font = new Font("Segoe UI", 64, FontStyle.Bold),
            ForeColor = Color.White,
            BackColor = Color.FromArgb(30, 136, 229),
            Size = new Size(120, 120),
            Location = new Point(190, 40),
            TextAlign = ContentAlignment.MiddleCenter
        };

        // Application name
        var appNameLabel = new Label
        {
            Text = "Lexon",
            Font = new Font("Segoe UI", 28, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 136, 229),
            Location = new Point(140, 170),
            AutoSize = true
        };

        // Tagline
        var taglineLabel = new Label
        {
            Text = "AI-Powered Typing Assistant",
            Font = new Font("Segoe UI", 10),
            ForeColor = Color.FromArgb(107, 107, 107),
            Location = new Point(115, 205),
            AutoSize = true
        };

        // Version label
        _versionLabel = new Label
        {
            Text = $"Version {GetApplicationVersion()}",
            Font = new Font("Segoe UI", 8),
            ForeColor = Color.FromArgb(149, 149, 149),
            Location = new Point(220, 225),
            AutoSize = true
        };

        // Progress bar
        _progressBar = new ProgressBar
        {
            Location = new Point(100, 250),
            Size = new Size(300, 8),
            Style = ProgressBarStyle.Continuous,
            BackColor = Color.FromArgb(30, 136, 229),
            ForeColor = Color.FromArgb(30, 136, 229)
        };

        // Status label
        _statusLabel = new Label
        {
            Text = "Initializing...",
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(107, 107, 107),
            Location = new Point(100, 265),
            AutoSize = true
        };

        // Add controls
        this.Controls.Add(_logoPictureBox);
        this.Controls.Add(logoText);
        this.Controls.Add(appNameLabel);
        this.Controls.Add(taglineLabel);
        this.Controls.Add(_versionLabel);
        this.Controls.Add(_progressBar);
        this.Controls.Add(_statusLabel);
    }

    /// <summary>
    /// Update loading progress
    /// </summary>
    public void UpdateProgress(int progress, string status)
    {
        if (this.InvokeRequired)
        {
            this.Invoke(new Action(() => UpdateProgress(progress, status)));
            return;
        }

        _loadingProgress = Math.Max(0, Math.Min(100, progress));
        _progressBar.Value = _loadingProgress;
        _statusLabel.Text = status;
    }

    /// <summary>
    /// Show splash screen and return after loading
    /// </summary>
    public static SplashScreen ShowAndLoad()
    {
        var splash = new SplashScreen();
        splash.Show();

        // Simulate loading steps
        Task.Run(async () =>
        {
            await Task.Delay(300);
            splash.UpdateProgress(20, "Loading components...");

            await Task.Delay(200);
            splash.UpdateProgress(40, "Initializing services...");

            await Task.Delay(200);
            splash.UpdateProgress(60, "Loading profiles...");

            await Task.Delay(200);
            splash.UpdateProgress(80, "Starting AI providers...");

            await Task.Delay(200);
            splash.UpdateProgress(100, "Ready!");

            await Task.Delay(300);
            splash.Close();
        });

        return splash;
    }

    private string GetApplicationVersion()
    {
        try
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";
        }
        catch
        {
            return "1.0.0";
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);

        // Draw background using solid color for simplicity
        var backgroundColor = Color.FromArgb(245, 247, 250);
        using var brush = new SolidBrush(backgroundColor);
        {
            e.Graphics.FillRectangle(brush, new Rectangle(0, 0, this.Width, this.Height));
        }
    }
}