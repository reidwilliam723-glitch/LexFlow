namespace LexFlow.Overlay.Interfaces;

public interface IActionMenuOverlay : IDisposable
{
    void ShowMenu(IReadOnlyList<string> items, int x, int y);
    void ShowMenu(IReadOnlyList<string> items, int x, int y, string? caption);
    void Hide();
    bool IsVisible { get; }
    void SelectNext();
    void SelectPrevious();
    void ConfirmSelection();
    void Cancel();
    event EventHandler<ActionMenuItemEventArgs>? ItemSelected;
    event EventHandler? Cancelled;
}

public class ActionMenuItemEventArgs : EventArgs
{
    public int Index { get; set; }
    public string Text { get; set; } = string.Empty;
}
