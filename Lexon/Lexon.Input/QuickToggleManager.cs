using Lexon.Input.Interfaces;

namespace Lexon.Input;

/// <summary>
/// Manages quick disable toggle (double-press Ctrl)
/// </summary>
public class QuickToggleManager
{
    private const int VkControl = 0x11;
    private const int VkLControl = 0xA2;
    private const int VkRControl = 0xA3;
    private const int DoublePressInterval = 400; // milliseconds

    private DateTime _lastCtrlPress = DateTime.MinValue;
    private bool _ctrlIsDown;
    private bool _isEnabled = true;
    
    public event EventHandler<bool>? ToggleStateChanged;

    public bool IsEnabled
    {
        get => _isEnabled;
        private set
        {
            if (_isEnabled != value)
            {
                _isEnabled = value;
                ToggleStateChanged?.Invoke(this, value);
            }
        }
    }

    public void HandleKeyPress(KeyboardEventArgs args)
    {
        if (!IsCtrlKey(args.VirtualKey) || args.IsShiftPressed || args.IsAltPressed)
        {
            return;
        }

        // WH_KEYBOARD_LL repeats KEYDOWN while Ctrl is held. Ignore those.
        if (_ctrlIsDown)
        {
            return;
        }

        _ctrlIsDown = true;
        var timeSinceLastPress = (DateTime.UtcNow - _lastCtrlPress).TotalMilliseconds;

        if (timeSinceLastPress < DoublePressInterval)
        {
            IsEnabled = !IsEnabled;
            _lastCtrlPress = DateTime.MinValue;
        }
        else
        {
            _lastCtrlPress = DateTime.UtcNow;
        }
    }

    public void HandleKeyRelease(KeyboardEventArgs args)
    {
        if (IsCtrlKey(args.VirtualKey))
        {
            _ctrlIsDown = false;
        }
    }

    internal static bool IsCtrlKey(int virtualKey) =>
        virtualKey is VkControl or VkLControl or VkRControl;

    public void SetEnabled(bool enabled)
    {
        IsEnabled = enabled;
    }

    /// <summary>
    /// Programmatically toggles the enabled state.
    /// Used by external triggers like tray menu clicks.
    /// </summary>
    public void Toggle()
    {
        IsEnabled = !IsEnabled;
    }
}
