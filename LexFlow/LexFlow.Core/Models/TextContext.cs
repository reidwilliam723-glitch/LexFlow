namespace LexFlow.Core.Models;

/// <summary>
/// Represents the current text context for suggestion generation
/// </summary>
public class TextContext
{
    public string CurrentWord { get; set; } = string.Empty;
    public string PreviousWords { get; set; } = string.Empty;
    public string FollowingWords { get; set; } = string.Empty;
    public string FullText { get; set; } = string.Empty;
    public int CursorPosition { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string WindowTitle { get; set; } = string.Empty;
    public bool IsPasswordField { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string SelectedText { get; set; } = string.Empty;
    public string Instruction { get; set; } = string.Empty;
    public string AppToneHint { get; set; } = string.Empty;
    public string WritingStyleHint { get; set; } = string.Empty;
    public AppWritingCategory AppCategory { get; set; } = AppWritingCategory.Neutral;
}
