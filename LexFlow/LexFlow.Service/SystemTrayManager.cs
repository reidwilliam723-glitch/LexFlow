using System.Drawing;
using System.Windows.Forms;

namespace LexFlow.Service;

/// <summary>
/// Manages the system tray icon with dynamic status indicators
/// </summary>
public class SystemTrayManager : IDisposable
{
    private NotifyIcon _notifyIcon = null!;
    private ContextMenuStrip _contextMenu = null!;
    private ServiceStatus _currentStatus = ServiceStatus.Inactive;
    private StatusToastForm? _statusToast;
    private System.Windows.Forms.Timer? _statusToastTimer;
    
    public event EventHandler? SettingsRequested;
    public event EventHandler? ExitRequested;
    public event EventHandler? ToggleRequested;
    public event EventHandler? KeyboardShortcutsRequested;
    public event EventHandler? UpdatesRequested;

    public SystemTrayManager()
    {
        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        _notifyIcon = new NotifyIcon
        {
            Text = "LexFlow - Inactive",
            Visible = true
        };
        
        _notifyIcon.Icon = CreateIcon(ServiceStatus.Inactive);
        
        _contextMenu = new ContextMenuStrip();
        
        var statusMenuItem = new ToolStripMenuItem("Status: Inactive");
        statusMenuItem.Enabled = false;
        
        _contextMenu.Items.Add(statusMenuItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        
        var toggleMenuItem = new ToolStripMenuItem("Enable LexFlow");
        toggleMenuItem.Click += (s, e) => ToggleRequested?.Invoke(this, EventArgs.Empty);
        
        var settingsMenuItem = new ToolStripMenuItem("Settings");
        settingsMenuItem.Click += (s, e) => SettingsRequested?.Invoke(this, EventArgs.Empty);
        
        var shortcutsMenuItem = new ToolStripMenuItem("Keyboard Shortcuts");
        shortcutsMenuItem.Click += (s, e) => KeyboardShortcutsRequested?.Invoke(this, EventArgs.Empty);

        var updatesMenuItem = new ToolStripMenuItem("Check for updates…");
        updatesMenuItem.Click += (s, e) => UpdatesRequested?.Invoke(this, EventArgs.Empty);
        
        var exitMenuItem = new ToolStripMenuItem("Exit");
        exitMenuItem.Click += (s, e) => ExitRequested?.Invoke(this, EventArgs.Empty);
        
        _contextMenu.Items.Add(toggleMenuItem);
        _contextMenu.Items.Add(settingsMenuItem);
        _contextMenu.Items.Add(shortcutsMenuItem);
        _contextMenu.Items.Add(updatesMenuItem);
        _contextMenu.Items.Add(new ToolStripSeparator());
        _contextMenu.Items.Add(exitMenuItem);
        
        _notifyIcon.ContextMenuStrip = _contextMenu;
        _notifyIcon.DoubleClick += (s, e) => SettingsRequested?.Invoke(this, EventArgs.Empty);

        // Ensure the menu has a window handle so Invoke/BeginInvoke works
        // before the user has ever opened the tray menu.
        _ = _contextMenu.Handle;
    }

    public void SetStatus(ServiceStatus status, string? additionalInfo = null)
    {
        _currentStatus = status;
        _notifyIcon.Icon = CreateIcon(status);
        
        var statusText = status switch
        {
            ServiceStatus.Active => "Active",
            ServiceStatus.Inactive => "Inactive",
            ServiceStatus.Error => "Error",
            ServiceStatus.Learning => "Learning",
            _ => "Unknown"
        };
        
        var tooltipText = $"LexFlow - {statusText}";
        if (!string.IsNullOrEmpty(additionalInfo))
        {
            tooltipText += $" ({additionalInfo})";
        }
        
        _notifyIcon.Text = tooltipText;
        
        if (_contextMenu.Items[0] is ToolStripMenuItem statusMenuItem)
        {
            statusMenuItem.Text = $"Status: {statusText}";
            if (!string.IsNullOrEmpty(additionalInfo))
            {
                statusMenuItem.Text += $" - {additionalInfo}";
            }
        }
        
        if (_contextMenu.Items[2] is ToolStripMenuItem toggleMenuItem)
        {
            toggleMenuItem.Text = status == ServiceStatus.Active ? "Disable LexFlow" : "Enable LexFlow";
        }
    }

    public void InvokeOnUiThread(Action action)
    {
        if (action == null || _contextMenu == null || _contextMenu.IsDisposed)
        {
            return;
        }

        if (!_contextMenu.IsHandleCreated)
        {
            _ = _contextMenu.Handle;
        }

        if (_contextMenu.InvokeRequired)
        {
            _contextMenu.BeginInvoke(action);
            return;
        }

        action();
    }

    public void ShowBalloonTip(string title, string text, ToolTipIcon icon = ToolTipIcon.Info)
    {
        _notifyIcon.ShowBalloonTip(3000, title, text, icon);
    }

    public void ShowStatusToast(string message, bool enabled)
    {
        InvokeOnUiThread(() => ShowStatusToastCore(message, enabled));
    }

    private void ShowStatusToastCore(string message, bool enabled)
    {
        _statusToastTimer?.Stop();
        _statusToast ??= new StatusToastForm();
        if (_statusToast.IsDisposed)
        {
            _statusToast = new StatusToastForm();
        }

        _statusToast.ShowMessage(message, enabled);

        _statusToastTimer ??= new System.Windows.Forms.Timer { Interval = 2200 };
        _statusToastTimer.Tick -= HideStatusToast;
        _statusToastTimer.Tick += HideStatusToast;
        _statusToastTimer.Start();
    }

    private void HideStatusToast(object? sender, EventArgs e)
    {
        _statusToastTimer?.Stop();
        if (_statusToast != null && !_statusToast.IsDisposed)
        {
            _statusToast.Hide();
        }
    }

    private Icon CreateIcon(ServiceStatus status)
    {
        // Create dynamic icons based on status
        var bitmap = new Bitmap(16, 16);
        using var graphics = Graphics.FromImage(bitmap);
        
        var color = status switch
        {
            ServiceStatus.Active => Color.FromArgb(0, 120, 215),
            ServiceStatus.Inactive => Color.FromArgb(128, 128, 128),
            ServiceStatus.Error => Color.FromArgb(217, 83, 79),
            ServiceStatus.Learning => Color.FromArgb(16, 185, 129),
            _ => Color.Gray
        };
        
        graphics.Clear(Color.Transparent);
        
        // Draw "L" logo
        using var brush = new SolidBrush(color);
        graphics.FillRectangle(brush, 2, 2, 3, 12);
        graphics.FillRectangle(brush, 2, 11, 12, 3);
        
        // Add indicator dot for learning
        if (status == ServiceStatus.Learning)
        {
            graphics.FillEllipse(Brushes.Green, 11, 11, 4, 4);
        }
        
        return Icon.FromHandle(bitmap.GetHicon());
    }

    public void Dispose()
    {
        _statusToastTimer?.Stop();
        _statusToastTimer?.Dispose();
        _statusToast?.Dispose();
        _notifyIcon?.Dispose();
        _contextMenu?.Dispose();
    }
}

public enum ServiceStatus
{
    Inactive,
    Active,
    Learning,
    Error
}
