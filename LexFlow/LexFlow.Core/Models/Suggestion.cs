namespace LexFlow.Core.Models;

/// <summary>
/// Represents a text suggestion with metadata
/// </summary>
public class Suggestion
{
    public string Text { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty; // Dictionary, Learned, AI
    public double Score { get; set; }
    public string? Category { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}
