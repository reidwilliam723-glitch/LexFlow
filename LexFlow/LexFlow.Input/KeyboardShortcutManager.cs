using LexFlow.Input.Interfaces;
using System.Runtime.InteropServices;

namespace LexFlow.Input;

/// <summary>
/// Manages keyboard shortcuts with conflict detection
/// </summary>
public class KeyboardShortcutManager
{
    private readonly Dictionary<string, KeyboardShortcut> _shortcuts = new();
    private readonly Dictionary<int, List<string>> _keyToShortcuts = new();
    private readonly Dictionary<string, EventHandler<KeyboardEventArgs>> _shortcutHandlers = new();
    
    public event EventHandler<ShortcutConflictEventArgs>? ConflictDetected;
    public event EventHandler<ShortcutTriggeredEventArgs>? ShortcutTriggered;

    public KeyboardShortcutManager()
    {
        InitializeDefaultShortcuts();
    }

    private void InitializeDefaultShortcuts()
    {
        RegisterShortcut("AcceptSuggestion", new KeyboardShortcut
        {
            Key = 9, // Tab
            Modifiers = KeyModifiers.None
        });

        RegisterShortcut("DismissSuggestion", new KeyboardShortcut
        {
            Key = 27, // Esc
            Modifiers = KeyModifiers.None
        });

        RegisterShortcut("NavigateUp", new KeyboardShortcut
        {
            Key = 38, // Up Arrow
            Modifiers = KeyModifiers.None
        });

        RegisterShortcut("NavigateDown", new KeyboardShortcut
        {
            Key = 40, // Down Arrow
            Modifiers = KeyModifiers.None
        });

        RegisterShortcut("QuickToggle", new KeyboardShortcut
        {
            Key = 17, // Ctrl
            Modifiers = KeyModifiers.None,
            IsDoubleClick = true
        });

        RegisterShortcut("OpenSettings", new KeyboardShortcut
        {
            Key = 83, // S
            Modifiers = KeyModifiers.Control | KeyModifiers.Shift
        });
    }

    public void RegisterShortcut(string name, KeyboardShortcut shortcut)
    {
        RegisterShortcut(name, shortcut, null);
    }

    public void RegisterShortcut(string name, KeyboardShortcut shortcut, EventHandler<KeyboardEventArgs>? handler)
    {
        // Check for conflicts
        var conflicts = DetectConflicts(shortcut);
        if (conflicts.Any())
        {
            ConflictDetected?.Invoke(this, new ShortcutConflictEventArgs
            {
                ShortcutName = name,
                Shortcut = shortcut,
                ConflictingShortcuts = conflicts
            });
            return;
        }

        // Remove old shortcut if exists
        if (_shortcuts.TryGetValue(name, out var oldShortcut))
        {
            UnregisterShortcut(name);
        }

        _shortcuts[name] = shortcut;
        
        if (!_keyToShortcuts.ContainsKey(shortcut.Key))
        {
            _keyToShortcuts[shortcut.Key] = new List<string>();
        }
        _keyToShortcuts[shortcut.Key].Add(name);

        if (handler != null)
        {
            _shortcutHandlers[name] = handler;
        }
    }

    public void UnregisterShortcut(string name)
    {
        if (_shortcuts.TryGetValue(name, out var shortcut))
        {
            _keyToShortcuts[shortcut.Key].Remove(name);
            if (_keyToShortcuts[shortcut.Key].Count == 0)
            {
                _keyToShortcuts.Remove(shortcut.Key);
            }
            _shortcuts.Remove(name);
            _shortcutHandlers.Remove(name);
        }
    }

    public void OnKeyPressed(KeyboardEventArgs args)
    {
        foreach (var (name, shortcut) in _shortcuts)
        {
            if (IsShortcutPressed(shortcut, args))
            {
                if (_shortcutHandlers.TryGetValue(name, out var handler))
                {
                    handler.Invoke(this, args);
                }
                ShortcutTriggered?.Invoke(this, new ShortcutTriggeredEventArgs
                {
                    ShortcutName = name,
                    Shortcut = shortcut,
                    EventArgs = args
                });
                break;
            }
        }
    }

    public KeyboardShortcut? GetShortcut(string name)
    {
        return _shortcuts.TryGetValue(name, out var shortcut) ? shortcut : null;
    }

    public string? GetShortcutName(KeyboardShortcut shortcut)
    {
        foreach (var (name, s) in _shortcuts)
        {
            if (s.Equals(shortcut))
            {
                return name;
            }
        }
        return null;
    }

    public IEnumerable<string> DetectConflicts(KeyboardShortcut shortcut)
    {
        var conflicts = new List<string>();
        
        if (_keyToShortcuts.TryGetValue(shortcut.Key, out var shortcutNames))
        {
            foreach (var name in shortcutNames)
            {
                if (_shortcuts.TryGetValue(name, out var existing) && existing.Equals(shortcut))
                {
                    conflicts.Add(name);
                }
            }
        }
        
        return conflicts;
    }

    public bool IsShortcutPressed(KeyboardShortcut shortcut, KeyboardEventArgs args)
    {
        if (args.VirtualKey != shortcut.Key) return false;
        
        var ctrlMatch = (args.IsControlPressed && (shortcut.Modifiers & KeyModifiers.Control) != 0) ||
                        (!args.IsControlPressed && (shortcut.Modifiers & KeyModifiers.Control) == 0);
        
        var shiftMatch = (args.IsShiftPressed && (shortcut.Modifiers & KeyModifiers.Shift) != 0) ||
                         (!args.IsShiftPressed && (shortcut.Modifiers & KeyModifiers.Shift) == 0);
        
        var altMatch = (args.IsAltPressed && (shortcut.Modifiers & KeyModifiers.Alt) != 0) ||
                       (!args.IsAltPressed && (shortcut.Modifiers & KeyModifiers.Alt) == 0);
        
        return ctrlMatch && shiftMatch && altMatch;
    }

    public string GetShortcutDisplayText(KeyboardShortcut shortcut)
    {
        var parts = new List<string>();
        
        if ((shortcut.Modifiers & KeyModifiers.Control) != 0)
            parts.Add("Ctrl");
        if ((shortcut.Modifiers & KeyModifiers.Shift) != 0)
            parts.Add("Shift");
        if ((shortcut.Modifiers & KeyModifiers.Alt) != 0)
            parts.Add("Alt");
        
        parts.Add(GetKeyName(shortcut.Key));
        
        return string.Join(" + ", parts);
    }

    private string GetKeyName(int virtualKey)
    {
        return virtualKey switch
        {
            9 => "Tab",
            27 => "Esc",
            38 => "Up",
            40 => "Down",
            37 => "Left",
            39 => "Right",
            13 => "Enter",
            32 => "Space",
            17 => "Ctrl",
            16 => "Shift",
            18 => "Alt",
            8 => "Backspace",
            46 => "Delete",
            36 => "Home",
            35 => "End",
            33 => "PageUp",
            34 => "PageDown",
            45 => "Insert",
            20 => "CapsLock",
            144 => "NumLock",
            145 => "ScrollLock",
            112 => "F1",
            113 => "F2",
            114 => "F3",
            115 => "F4",
            116 => "F5",
            117 => "F6",
            118 => "F7",
            119 => "F8",
            120 => "F9",
            121 => "F10",
            122 => "F11",
            123 => "F12",
            _ when virtualKey >= 0x41 && virtualKey <= 0x5A => ((char)virtualKey).ToString(),
            _ when virtualKey >= 0x30 && virtualKey <= 0x39 => ((char)virtualKey).ToString(),
            _ => $"VK_{virtualKey:X2}"
        };
    }
}

public class KeyboardShortcut
{
    public int Key { get; set; }
    public KeyModifiers Modifiers { get; set; }
    public bool IsDoubleClick { get; set; }
    public DateTime LastPressed { get; set; }

    public bool Equals(KeyboardShortcut other)
    {
        if (other == null) return false;
        return Key == other.Key && Modifiers == other.Modifiers && IsDoubleClick == other.IsDoubleClick;
    }
}

[Flags]
public enum KeyModifiers
{
    None = 0,
    Control = 1,
    Shift = 2,
    Alt = 4
}

public class ShortcutConflictEventArgs : EventArgs
{
    public string ShortcutName { get; set; } = string.Empty;
    public KeyboardShortcut Shortcut { get; set; } = null!;
    public IEnumerable<string> ConflictingShortcuts { get; set; } = Enumerable.Empty<string>();
}

public class ShortcutTriggeredEventArgs : EventArgs
{
    public string ShortcutName { get; set; } = string.Empty;
    public KeyboardShortcut Shortcut { get; set; } = null!;
    public KeyboardEventArgs EventArgs { get; set; } = null!;
}
