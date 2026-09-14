using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace LexFlow.Overlay.Accessibility;

/// <summary>
/// Screen reader compatibility for suggestions
/// </summary>
public class ScreenReaderSupport
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("oleacc.dll")]
    private static extern IntPtr AccessibleObjectFromWindow(IntPtr hwnd, uint idObject, ref Guid riid, IntPtr ppvObject);

    [DllImport("user32.dll")]
    private static extern bool NotifyWinEvent(uint winEvent, IntPtr hwnd, uint idObject, uint idChild);

    private const uint EVENT_OBJECT_FOCUS = 0x8005;
    private const uint EVENT_OBJECT_SHOW = 0x8002;
    private const uint EVENT_OBJECT_HIDE = 0x8003;
    private const uint OBJID_CLIENT = 0xFFFFFFFC;

    public static void AnnounceSuggestion(string suggestion, string source)
    {
        var announcement = $"Suggestion: {suggestion}. Source: {source}";
        AnnounceToScreenReader(announcement);
    }

    public static void AnnounceSuggestionList(IEnumerable<string> suggestions)
    {
        var count = suggestions.Count();
        var announcement = $"{count} suggestions available. ";
        
        if (count > 0)
        {
            announcement += $"First suggestion: {suggestions.First()}";
        }
        
        AnnounceToScreenReader(announcement);
    }

    public static void AnnounceNavigation(int currentIndex, int total)
    {
        var announcement = $"Suggestion {currentIndex + 1} of {total}";
        AnnounceToScreenReader(announcement);
    }

    public static void AnnounceSuggestionAccepted(string suggestion)
    {
        var announcement = $"Accepted: {suggestion}";
        AnnounceToScreenReader(announcement);
    }

    public static void AnnounceSuggestionDismissed()
    {
        AnnounceToScreenReader("Suggestions dismissed");
    }

    public static void AnnounceError(string error)
    {
        AnnounceToScreenReader($"Error: {error}");
    }

    private static void AnnounceToScreenReader(string text)
    {
        try
        {
            // Method 1: Use Windows UI Automation
            var foregroundWindow = GetForegroundWindow();
            if (foregroundWindow != IntPtr.Zero)
            {
                NotifyWinEvent(EVENT_OBJECT_SHOW, foregroundWindow, OBJID_CLIENT, 0);
            }

            // Method 2: Set accessible name on a control
            // This would be called by the suggestion overlay to set its accessible name
        }
        catch
        {
            // Silently fail if screen reader announcement fails
        }
    }
}

public class AccessibleSuggestionControl : Control
{
    private string _accessibleName = string.Empty;
    private string _accessibleDescription = string.Empty;

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new string AccessibleName
    {
        get => _accessibleName;
        set
        {
            _accessibleName = value;
            UpdateAccessibilityInfo();
        }
    }

    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public new string AccessibleDescription
    {
        get => _accessibleDescription;
        set
        {
            _accessibleDescription = value;
            UpdateAccessibilityInfo();
        }
    }

    protected override AccessibleObject CreateAccessibilityInstance()
    {
        return new SuggestionAccessibleObject(this);
    }

    private void UpdateAccessibilityInfo()
    {
        // Update accessibility information for screen readers
        this.AccessibleName = _accessibleName;
        this.AccessibleDescription = _accessibleDescription;
    }
}

public class SuggestionAccessibleObject : Control.ControlAccessibleObject
{
    public SuggestionAccessibleObject(Control owner) : base(owner)
    {
    }

    public override string Name
    {
        get => base.Name;
        set => base.Name = value;
    }

    public override string Description
    {
        get => base.Description;
    }

    public override string DefaultAction
    {
        get => "Accept suggestion";
    }

    public override void DoDefaultAction()
    {
        // Trigger the suggestion acceptance
        base.DoDefaultAction();
    }
}
