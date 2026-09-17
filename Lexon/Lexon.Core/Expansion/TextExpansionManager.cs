using Lexon.Core.Interfaces;
using Lexon.Core.Models;
using System.Text.Json;
using System.Linq;
using System.Text.RegularExpressions;

namespace Lexon.Core.Expansion;

/// <summary>
/// Manages text expansion triggers and expansions
/// </summary>
public class TextExpansionManager
{
    private readonly IStorage _storage;
    private readonly Dictionary<string, TextExpansion> _expansions;
    private string _currentTyping = string.Empty;
    private bool _isEnabled = true;
    private const string ExpansionsKey = "text_expansions";

    public event EventHandler<TextExpansionTriggeredEventArgs>? ExpansionTriggered;

    public bool IsEnabled => _isEnabled;

    public void SetEnabled(bool enabled)
    {
        _isEnabled = enabled;
    }

    public TextExpansionManager(IStorage storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _expansions = new Dictionary<string, TextExpansion>(StringComparer.OrdinalIgnoreCase);
        LoadExpansions();
    }

    public void AddExpansion(TextExpansion expansion)
    {
        if (expansion == null) throw new ArgumentNullException(nameof(expansion));
        if (string.IsNullOrWhiteSpace(expansion.Trigger))
            throw new ArgumentException("Trigger cannot be empty", nameof(expansion));

        // Save version history if updating existing expansion
        if (_expansions.ContainsKey(expansion.Trigger))
        {
            var existing = _expansions[expansion.Trigger];
            if (existing.Expansion != expansion.Expansion)
            {
                expansion.VersionHistory = new List<string>(existing.VersionHistory);
                expansion.VersionHistory.Add(existing.Expansion);
            }
        }

        expansion.UpdatedAt = DateTime.UtcNow;
        _expansions[expansion.Trigger] = expansion;
        SaveExpansions();
    }

    public void RemoveExpansion(string trigger)
    {
        if (_expansions.Remove(trigger))
        {
            SaveExpansions();
        }
    }

    public TextExpansion? GetExpansion(string trigger)
    {
        return _expansions.TryGetValue(trigger, out var expansion) ? expansion : null;
    }

    public IEnumerable<TextExpansion> GetAllExpansions()
    {
        return _expansions.Values;
    }

    public void OnCharacterTyped(char character)
    {
        if (!_isEnabled)
        {
            return;
        }

        if (char.IsControl(character))
        {
            // Handle backspace
            if (character == '\b' && _currentTyping.Length > 0)
            {
                _currentTyping = _currentTyping.Substring(0, _currentTyping.Length - 1);
            }
            // Handle space, enter, tab - these trigger expansion check
            else if (character == ' ' || character == '\n' || character == '\r' || character == '\t')
            {
                CheckForExpansion();
                _currentTyping = string.Empty;
            }
            return;
        }

        _currentTyping += character;

        // Check for trigger match after each character
        CheckForTrigger();
    }

    private void CheckForTrigger()
    {
        foreach (var expansion in _expansions.Values.Where(e => e.IsEnabled))
        {
            if (_currentTyping.EndsWith(expansion.Trigger, StringComparison.OrdinalIgnoreCase)
                && IsTriggerAtWordBoundary(_currentTyping, expansion.Trigger.Length))
            {
                TriggerExpansion(expansion);
                _currentTyping = string.Empty;
                return;
            }
        }
    }

    private static bool IsTriggerAtWordBoundary(string buffer, int triggerLength)
    {
        var triggerStart = buffer.Length - triggerLength;
        if (triggerStart <= 0)
        {
            return true;
        }

        var charBeforeTrigger = buffer[triggerStart - 1];
        return !char.IsLetterOrDigit(charBeforeTrigger);
    }

    private void CheckForExpansion()
    {
        // Check if the current typing matches any trigger exactly
        foreach (var expansion in _expansions.Values.Where(e => e.IsEnabled))
        {
            if (_currentTyping.Equals(expansion.Trigger, StringComparison.OrdinalIgnoreCase))
            {
                TriggerExpansion(expansion);
                return;
            }
        }
    }

    private void TriggerExpansion(TextExpansion expansion)
    {
        expansion.UsageCount++;
        SaveExpansions();
        var processedExpansion = ProcessExpansionVariables(expansion);
        
        var args = new TextExpansionTriggeredEventArgs
        {
            Expansion = expansion,
            FullText = _currentTyping,
            VariableValues = expansion.Variables
        };

        ExpansionTriggered?.Invoke(this, args);
    }

    /// <summary>
    /// Process variables in expansion text and replace with values
    /// </summary>
    private string ProcessExpansionVariables(TextExpansion expansion)
    {
        if (!expansion.SupportsVariables || expansion.VariableDefinitions.Count == 0)
        {
            return expansion.Expansion;
        }

        var result = expansion.Expansion;
        
        foreach (var variableDef in expansion.VariableDefinitions)
        {
            var placeholder = $"{{{{{variableDef.Name}}}}}";
            var value = GetVariableValue(variableDef, expansion);
            result = result.Replace(placeholder, value);
        }

        return result;
    }

    /// <summary>
    /// Get the value for a variable, using default if not provided
    /// </summary>
    private string GetVariableValue(ExpansionVariable variableDef, TextExpansion expansion)
    {
        // Check if user provided a value
        if (expansion.Variables.TryGetValue(variableDef.Name, out var userValue) && 
            !string.IsNullOrEmpty(userValue))
        {
            return FormatVariableValue(userValue, variableDef.Type);
        }

        // Use default value
        if (!string.IsNullOrEmpty(variableDef.DefaultValue))
        {
            return FormatVariableValue(variableDef.DefaultValue, variableDef.Type);
        }

        // Handle special system variables
        return GetSystemVariableValue(variableDef.Name);
    }

    /// <summary>
    /// Format variable value based on its type
    /// </summary>
    private string FormatVariableValue(string value, VariableType type)
    {
        return type switch
        {
            VariableType.Date => FormatDateValue(value),
            VariableType.Email => value.ToLowerInvariant(),
            VariableType.Phone => FormatPhoneValue(value),
            VariableType.Url => EnsureUrlProtocol(value),
            VariableType.Number => value,
            _ => value
        };
    }

    /// <summary>
    /// Format date value
    /// </summary>
    private string FormatDateValue(string value)
    {
        if (DateTime.TryParse(value, out var date))
        {
            return date.ToString("MMMM dd, yyyy");
        }
        return value;
    }

    /// <summary>
    /// Format phone number
    /// </summary>
    private string FormatPhoneValue(string value)
    {
        var digits = new string(value.Where(char.IsDigit).ToArray());
        if (digits.Length == 10)
        {
            return $"({digits.Substring(0, 3)}) {digits.Substring(3, 3)}-{digits.Substring(6)}";
        }
        return value;
    }

    /// <summary>
    /// Ensure URL has protocol
    /// </summary>
    private string EnsureUrlProtocol(string value)
    {
        if (!value.StartsWith("http://", StringComparison.OrdinalIgnoreCase) && 
            !value.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return $"https://{value}";
        }
        return value;
    }

    /// <summary>
    /// Get system variable values (date, time, user, etc.)
    /// </summary>
    private string GetSystemVariableValue(string variableName)
    {
        return variableName.ToLowerInvariant() switch
        {
            "date" => DateTime.Now.ToString("MMMM dd, yyyy"),
            "time" => DateTime.Now.ToString("hh:mm tt"),
            "datetime" => DateTime.Now.ToString("MMMM dd, yyyy hh:mm tt"),
            "year" => DateTime.Now.Year.ToString(),
            "month" => DateTime.Now.ToString("MMMM"),
            "day" => DateTime.Now.Day.ToString(),
            "username" => Environment.UserName,
            "computername" => Environment.MachineName,
            "clipboard" => GetClipboardText(),
            _ => string.Empty
        };
    }

    /// <summary>
    /// Get text from clipboard
    /// </summary>
    private string GetClipboardText()
    {
        try
        {
            // Use Windows Forms clipboard if available
            var clipboardType = Type.GetType("System.Windows.Forms.Clipboard, System.Windows.Forms");
            if (clipboardType != null)
            {
                var containsTextMethod = clipboardType.GetMethod("ContainsText");
                var getTextMethod = clipboardType.GetMethod("GetText");
                
                if (containsTextMethod != null && getTextMethod != null)
                {
                    var containsText = (bool)containsTextMethod.Invoke(null, null);
                    if (containsText)
                    {
                        return (string)getTextMethod.Invoke(null, null) ?? string.Empty;
                    }
                }
            }
        }
        catch
        {
            // Clipboard access might fail or Forms not available
        }
        return string.Empty;
    }

    /// <summary>
    /// Set variable value for an expansion
    /// </summary>
    public void SetExpansionVariable(string trigger, string variableName, string value)
    {
        if (_expansions.TryGetValue(trigger, out var expansion))
        {
            expansion.Variables[variableName] = value;
            SaveExpansions();
        }
    }

    /// <summary>
    /// Add variable definition to an expansion
    /// </summary>
    public void AddVariableDefinition(string trigger, ExpansionVariable variable)
    {
        if (_expansions.TryGetValue(trigger, out var expansion))
        {
            expansion.SupportsVariables = true;
            expansion.VariableDefinitions.Add(variable);
            SaveExpansions();
        }
    }

    /// <summary>
    /// Parse expansion text to find variable placeholders
    /// </summary>
    public List<string> FindVariablePlaceholders(string expansionText)
    {
        var placeholders = new List<string>();
        var pattern = @"\{\{([^}]+)\}\}";
        var matches = Regex.Matches(expansionText, pattern);
        
        foreach (Match match in matches)
        {
            placeholders.Add(match.Groups[1].Value);
        }
        
        return placeholders;
    }

    public void ClearTypingBuffer()
    {
        _currentTyping = string.Empty;
    }

    public async Task LoadExpansionsAsync()
    {
        try
        {
            var data = await _storage.LoadAsync<string>(ExpansionsKey);
            if (!string.IsNullOrEmpty(data))
            {
                var loadedExpansions = JsonSerializer.Deserialize<List<TextExpansion>>(data);
                if (loadedExpansions != null)
                {
                    foreach (var expansion in loadedExpansions)
                    {
                        _expansions[expansion.Trigger] = expansion;
                    }
                }
            }
        }
        catch
        {
            // If loading fails, start with empty expansions
        }

        // Add some default expansions if empty
        if (_expansions.Count == 0)
        {
            InitializeDefaultExpansions();
        }
    }

    private void LoadExpansions()
    {
        LoadExpansionsAsync().GetAwaiter().GetResult();
    }

    private void SaveExpansions()
    {
        try
        {
            var data = JsonSerializer.Serialize(_expansions.Values.ToList());
            _storage.SaveAsync(ExpansionsKey, data).GetAwaiter().GetResult();
        }
        catch
        {
            // Silently fail if save fails
        }
    }

    private void InitializeDefaultExpansions()
    {
        var defaultExpansions = new[]
        {
            new TextExpansion
            {
                Trigger = "addr",
                Expansion = "123 Main Street, Anytown, USA 12345",
                Description = "Address placeholder"
            },
            new TextExpansion
            {
                Trigger = "email",
                Expansion = "user@example.com",
                Description = "Email placeholder"
            },
            new TextExpansion
            {
                Trigger = "phone",
                Expansion = "+1 (555) 123-4567",
                Description = "Phone number placeholder"
            },
            new TextExpansion
            {
                Trigger = "sig",
                Expansion = "Best regards,\n{{username}}\n{{title}}",
                Description = "Email signature with variables",
                SupportsVariables = true,
                VariableDefinitions = new List<ExpansionVariable>
                {
                    new ExpansionVariable { Name = "username", DefaultValue = "[Your Name]", Description = "Your name" },
                    new ExpansionVariable { Name = "title", DefaultValue = "[Your Title]", Description = "Your job title" }
                }
            },
            new TextExpansion
            {
                Trigger = "date",
                Expansion = "{{date}}",
                Description = "Current date",
                SupportsVariables = true,
                VariableDefinitions = new List<ExpansionVariable>
                {
                    new ExpansionVariable { Name = "date", Description = "Current date", Type = VariableType.Date }
                }
            },
            new TextExpansion
            {
                Trigger = "meeting",
                Expansion = "Meeting on {{date}} at {{time}}\nAgenda: {{agenda}}\nAttendees: {{attendees}}",
                Description = "Meeting template with variables",
                SupportsVariables = true,
                VariableDefinitions = new List<ExpansionVariable>
                {
                    new ExpansionVariable { Name = "date", Description = "Meeting date", Type = VariableType.Date },
                    new ExpansionVariable { Name = "time", Description = "Meeting time", DefaultValue = "2:00 PM" },
                    new ExpansionVariable { Name = "agenda", Description = "Meeting agenda", IsRequired = true },
                    new ExpansionVariable { Name = "attendees", Description = "Meeting attendees", DefaultValue = "TBD" }
                }
            }
        };

        foreach (var expansion in defaultExpansions)
        {
            AddExpansion(expansion);
        }
    }

    public int ExpansionCount => _expansions.Count;
}
