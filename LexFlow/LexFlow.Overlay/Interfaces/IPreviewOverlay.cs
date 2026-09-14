namespace LexFlow.Overlay.Interfaces;

public interface IPreviewOverlay : IDisposable
{
    void ShowPreview(string before, string after, int x, int y);
    void ShowLivePreview(string before, int x, int y);
    void UpdateAfter(string after, bool complete);
    void Hide();
    bool IsVisible { get; }
    void Confirm();
    void Cancel();
    event EventHandler? Confirmed;
    event EventHandler? Cancelled;
}
