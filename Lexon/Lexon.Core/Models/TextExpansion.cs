namespace Lexon.Core.Models;

/// <summary>
/// Represents a text expansion (snippet trigger)
/// </summary>
public class TextExpansion
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Trigger { get; set; } = string.Empty;
    public string Expansion { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<string> VersionHistory { get; set; } = new();
    public bool IsEnabled { get; set; } = true;
    
    // Advanced features
    public Dictionary<string, string> Variables { get; set; } = new();
    public bool SupportsVariables { get; set; } = false;
    public List<ExpansionVariable> VariableDefinitions { get; set; } = new();
    public int UsageCount { get; set; }
}

/// <summary>
/// Defines a variable that can be used in text expansions
/// </summary>
public class ExpansionVariable
{
    public string Name { get; set; } = string.Empty;
    public string DefaultValue { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool IsRequired { get; set; } = false;
    public VariableType Type { get; set; } = VariableType.Text;
}

/// <summary>
/// Types of variables supported in expansions
/// </summary>
public enum VariableType
{
    Text,
    Number,
    Date,
    Email,
    Phone,
    Url,
    Custom
}

/// <summary>
/// Event args for text expansion triggered
/// </summary>
public class TextExpansionTriggeredEventArgs : EventArgs
{
    public TextExpansion Expansion { get; set; } = null!;
    public string FullText { get; set; } = string.Empty;
    public Dictionary<string, string> VariableValues { get; set; } = new();
}
