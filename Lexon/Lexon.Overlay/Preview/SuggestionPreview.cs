using Lexon.Core.Models;
using System.Drawing;

namespace Lexon.Overlay.Preview;

/// <summary>
/// Suggestion preview panel that shows what will be inserted before acceptance
/// </summary>
public class SuggestionPreview : Form
{
    private Suggestion? _currentSuggestion;
    private string _currentText = string.Empty;
    private Label _previewLabel = null!;
    private Label _diffLabel = null!;

    public SuggestionPreview()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        this.FormBorderStyle = FormBorderStyle.None;
        this.TopMost = true;
        this.ShowInTaskbar = false;
        this.BackColor = Color.FromArgb(255, 255, 240);
        this.Size = new Size(400, 100);
        this.StartPosition = FormStartPosition.Manual;

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(10)
        };

        _previewLabel = new Label
        {
            Text = "Preview:",
            Font = new Font("Segoe UI", 8, FontStyle.Bold),
            Location = new Point(10, 10),
            AutoSize = true
        };

        _diffLabel = new Label
        {
            Location = new Point(10, 30),
            Size = new Size(380, 60),
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(50, 50, 50)
        };

        panel.Controls.Add(_previewLabel);
        panel.Controls.Add(_diffLabel);
        this.Controls.Add(panel);
    }

    public void ShowPreview(Suggestion suggestion, string currentText, int x, int y)
    {
        _currentSuggestion = suggestion;
        _currentText = currentText;

        // Calculate what the text will look like after insertion
        var previewText = GetPreviewText(currentText, suggestion.Text);
        _diffLabel.Text = previewText;

        // Position above the cursor
        this.Location = new Point(x, y - 110);
        this.Show();
    }

    public void HidePreview()
    {
        this.Hide();
    }

    private string GetPreviewText(string currentText, string suggestionText)
    {
        // Simple preview - in a real implementation, this would show the actual diff
        var lastWord = GetLastWord(currentText);
        var beforeLastWord = currentText.Substring(0, currentText.Length - lastWord.Length);
        
        return $"{beforeLastWord}[{lastWord} → {suggestionText}]";
    }

    private string GetLastWord(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return string.Empty;
        
        var words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        return words.Length > 0 ? words[^1] : string.Empty;
    }

    protected override bool ShowWithoutActivation
    {
        get { return true; }
    }

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_NOACTIVATE = 0x08000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_NOACTIVATE;
            return cp;
        }
    }
}
