namespace LexFlow.Input.Interfaces;

/// <summary>
/// Interface for injecting text into the focused application
/// </summary>
public interface ITextInjector
{
    /// <summary>
    /// Injects the specified text into the focused application
    /// </summary>
    void InjectText(string text);

    /// <summary>
    /// Deletes the specified number of characters backward
    /// </summary>
    void DeleteBackward(int count);

    /// <summary>
    /// Replaces old text with new text
    /// </summary>
    void ReplaceText(string oldText, string newText);

    /// <summary>
    /// Injects text via clipboard as a fallback method
    /// </summary>
    void InjectTextViaClipboard(string text);

    /// <summary>
    /// Extends the selection backward by whole words (Ctrl+Shift+Left).
    /// Used in browsers / Google Docs where batched Backspace is dropped.
    /// </summary>
    void SelectBackwardWords(int wordCount);

    void Flush();
}
