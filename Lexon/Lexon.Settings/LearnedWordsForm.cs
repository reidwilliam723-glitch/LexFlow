using Lexon.Core.Pipeline;
using Lexon.Core.Theming;

namespace Lexon.Settings;

public class LearnedWordsForm : Form
{
    private readonly SuggestionPipeline? _pipeline;
    private readonly ThemeManager? _themeManager;
    private readonly ListBox _list = new();
    private readonly TextBox _addBox = new();

    public LearnedWordsForm(SuggestionPipeline? pipeline, ThemeManager? themeManager)
    {
        _pipeline = pipeline;
        _themeManager = themeManager;
        InitializeComponent();
        ApplyTheme();
        RefreshList();
        if (_themeManager != null)
        {
            _themeManager.ThemeChanged += OnExternalThemeChanged;
            FormClosed += (_, _) => _themeManager.ThemeChanged -= OnExternalThemeChanged;
        }
    }

    private void OnExternalThemeChanged(object? sender, ThemeChangedEventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(OnExternalThemeChanged, sender, e);
            return;
        }

        ApplyTheme();
    }

    private void InitializeComponent()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "Learned words";
        ClientSize = new Size(520, 460);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ThemeUi.EnableBufferedPaint(this);
        Font = new Font("Segoe UI", 9);

        var intro = new Label
        {
            Text = "Words you have typed often enough to be suggested. Dictionary words are not listed here.",
            Location = new Point(20, 16),
            Size = new Size(480, 36)
        };
        Controls.Add(intro);

        _list.Location = new Point(20, 60);
        _list.Size = new Size(360, 300);
        Controls.Add(_list);

        var delete = CreateButton("Delete", 396, 60);
        delete.Click += (_, _) => RunOnSelected(word => _pipeline!.RemoveLearnedWord(word));
        Controls.Add(delete);

        var never = CreateButton("Never learn", 396, 104);
        never.Click += (_, _) => RunOnSelected(word => _pipeline!.NeverLearnWord(word));
        Controls.Add(never);

        var undo = CreateButton("Undo last", 396, 148);
        undo.Click += (_, _) =>
        {
            _pipeline?.UndoLastLearn();
            RefreshList();
        };
        Controls.Add(undo);

        var clear = CreateButton("Clear all", 396, 192);
        clear.Click += OnClearAll;
        Controls.Add(clear);

        _addBox.Location = new Point(20, 376);
        _addBox.Size = new Size(250, 27);
        _addBox.PlaceholderText = "Add a custom word";
        Controls.Add(_addBox);

        var add = CreateButton("Add", 280, 372);
        add.Click += OnAdd;
        Controls.Add(add);

        var close = CreateButton("Close", 396, 412);
        close.Click += (_, _) => Close();
        Controls.Add(close);
    }

    private static Button CreateButton(string text, int x, int y)
    {
        return new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(108, 36)
        };
    }

    private void RunOnSelected(Action<string> action)
    {
        if (_pipeline == null || _list.SelectedItem is not string word || string.IsNullOrWhiteSpace(word))
        {
            return;
        }

        action(word);
        RefreshList();
    }

    private void OnAdd(object? sender, EventArgs e)
    {
        var word = _addBox.Text.Trim();
        if (string.IsNullOrEmpty(word) || _pipeline == null)
        {
            return;
        }

        _pipeline.AddExplicitLearnedWord(word);
        _addBox.Clear();
        RefreshList();
    }

    private void OnClearAll(object? sender, EventArgs e)
    {
        if (_pipeline == null)
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            "Remove every learned custom word? Dictionary words are not affected.",
            "Clear learned words",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
        {
            return;
        }

        _pipeline.ClearLearnedWords();
        RefreshList();
    }

    private void RefreshList()
    {
        _list.BeginUpdate();
        try
        {
            _list.Items.Clear();
            if (_pipeline == null)
            {
                return;
            }

            var count = 0;
            foreach (var word in _pipeline.GetLearnedWords())
            {
                _list.Items.Add(word);
                count++;
                if (count >= 500)
                {
                    _list.Items.Add("… (list truncated)");
                    break;
                }
            }
        }
        catch
        {
            // Listing must not crash the dialog.
        }
        finally
        {
            _list.EndUpdate();
        }
    }

    private void ApplyTheme()
    {
        if (_themeManager == null)
        {
            return;
        }

        ThemeUi.ApplyToTreeWithoutFlicker(this, _themeManager.CurrentTheme);
    }
}
