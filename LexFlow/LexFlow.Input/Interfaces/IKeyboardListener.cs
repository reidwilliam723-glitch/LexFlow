namespace LexFlow.Input.Interfaces;

/// <summary>
/// Interface for global keyboard input listening
/// </summary>
public interface IKeyboardListener
{
    event EventHandler<KeyboardEventArgs>? KeyPressed;
    event EventHandler<KeyboardEventArgs>? KeyReleased;
    void Start();
    void Stop();
}

public class KeyboardEventArgs : EventArgs
{
    public int VirtualKey { get; set; }
    public bool IsShiftPressed { get; set; }
    public bool IsControlPressed { get; set; }
    public bool IsAltPressed { get; set; }
    /// <summary>
    /// Set to true from a KeyPressed handler to swallow the key so the target app does not see it.
    /// </summary>
    public bool Handled { get; set; }
}
