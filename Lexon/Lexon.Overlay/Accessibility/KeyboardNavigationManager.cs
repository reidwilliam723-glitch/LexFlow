using System.Windows.Forms;

namespace Lexon.Overlay.Accessibility;

/// <summary>
/// Ensures keyboard-only navigation throughout the UI
/// </summary>
public class KeyboardNavigationManager
{
    private readonly Form _ownerForm;
    private readonly List<Control> _navigableControls = new();
    private int _currentFocusIndex = 0;
    private bool _keyboardNavigationEnabled = true;

    public KeyboardNavigationManager(Form ownerForm)
    {
        _ownerForm = ownerForm ?? throw new ArgumentNullException(nameof(ownerForm));
        InitializeNavigation();
    }

    private void InitializeNavigation()
    {
        _ownerForm.KeyDown += OnKeyDown;
        _ownerForm.Load += OnFormLoad;
    }

    private void OnFormLoad(object? sender, EventArgs e)
    {
        // Automatically register all controls with TabStop
        RegisterNavigableControls(_ownerForm);
    }

    public void RegisterNavigableControls(Control container)
    {
        _navigableControls.Clear();
        
        foreach (var control in GetAllControls(container))
        {
            if (IsNavigable(control))
            {
                control.TabStop = true;
                control.TabIndex = _navigableControls.Count;
                _navigableControls.Add(control);
                
                // Ensure keyboard accessibility
                if (control is Button button)
                {
                    button.KeyDown += OnControlKeyDown;
                }
                else if (control is CheckBox checkBox)
                {
                    checkBox.KeyDown += OnControlKeyDown;
                }
                else if (control is ComboBox comboBox)
                {
                    comboBox.KeyDown += OnControlKeyDown;
                }
                else if (control is TextBox textBox)
                {
                    textBox.KeyDown += OnControlKeyDown;
                }
            }
        }
    }

    private IEnumerable<Control> GetAllControls(Control container)
    {
        var controls = new List<Control>();
        foreach (Control control in container.Controls)
        {
            controls.Add(control);
            controls.AddRange(GetAllControls(control));
        }
        return controls;
    }

    private bool IsNavigable(Control control)
    {
        return control is Button || 
               control is CheckBox || 
               control is ComboBox || 
               control is TextBox || 
               control is RadioButton ||
               control is ListBox ||
               control is ListView;
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_keyboardNavigationEnabled) return;

        switch (e.KeyCode)
        {
            case Keys.Tab:
                HandleTabNavigation(e.Shift);
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
                
            case Keys.Enter:
                HandleEnterKey();
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
                
            case Keys.Escape:
                HandleEscapeKey();
                e.Handled = true;
                e.SuppressKeyPress = true;
                break;
        }
    }

    private void OnControlKeyDown(object? sender, KeyEventArgs e)
    {
        if (!_keyboardNavigationEnabled) return;

        if (e.KeyCode == Keys.Enter && sender is Control control)
        {
            // Simulate button click for Enter key
            if (control is Button button)
            {
                button.PerformClick();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        }
    }

    private void HandleTabNavigation(bool shiftPressed)
    {
        if (_navigableControls.Count == 0) return;

        if (shiftPressed)
        {
            _currentFocusIndex = (_currentFocusIndex - 1 + _navigableControls.Count) % _navigableControls.Count;
        }
        else
        {
            _currentFocusIndex = (_currentFocusIndex + 1) % _navigableControls.Count;
        }

        _navigableControls[_currentFocusIndex].Focus();
    }

    private void HandleEnterKey()
    {
        // Activate the currently focused control
        if (_currentFocusIndex >= 0 && _currentFocusIndex < _navigableControls.Count)
        {
            var control = _navigableControls[_currentFocusIndex];
            if (control is Button button)
            {
                button.PerformClick();
            }
            else if (control is CheckBox checkBox)
            {
                checkBox.Checked = !checkBox.Checked;
            }
        }
    }

    private void HandleEscapeKey()
    {
        // Close the form or cancel current operation
        _ownerForm.Close();
    }

    public void SetFocusToFirstControl()
    {
        if (_navigableControls.Count > 0)
        {
            _currentFocusIndex = 0;
            _navigableControls[0].Focus();
        }
    }

    public void SetFocusToControl(Control control)
    {
        var index = _navigableControls.IndexOf(control);
        if (index >= 0)
        {
            _currentFocusIndex = index;
            control.Focus();
        }
    }

    public void EnableKeyboardNavigation(bool enabled)
    {
        _keyboardNavigationEnabled = enabled;
    }

    public void SetShortcutKeys(Dictionary<Keys, Action> shortcuts)
    {
        _ownerForm.KeyDown += (sender, e) =>
        {
            if (shortcuts.TryGetValue(e.KeyCode, out var action))
            {
                action();
                e.Handled = true;
                e.SuppressKeyPress = true;
            }
        };
    }
}
