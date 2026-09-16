using LexFlow.Core;
using LexFlow.Core.Expansion;
using LexFlow.Core.Learning;
using LexFlow.Core.Theming;

namespace LexFlow.Settings;

public class WritingStatsForm : Form
{
    private readonly PersonalizationManager? _personalization;
    private readonly TextExpansionManager? _expansions;
    private readonly ThemeManager? _themeManager;

    private readonly Label _unavailable = new();
    private readonly Panel _content = new();
    private readonly Label _accepted = ValueLabel();
    private readonly Label _expansionUses = ValueLabel();
    private readonly Label _characters = ValueLabel();
    private readonly Label _timeSaved = ValueLabel();
    private readonly ListView _topExpansions = new();

    public WritingStatsForm(
        PersonalizationManager? personalization,
        TextExpansionManager? expansions,
        ThemeManager? themeManager)
    {
        _personalization = personalization;
        _expansions = expansions;
        _themeManager = themeManager;
        InitializeComponent();
        ApplyTheme();
        RefreshStats();
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
        Text = "Writing Statistics";
        MinimumSize = new Size(820, 680);
        Size = new Size(980, 820);
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = false;
        ShowInTaskbar = false;
        Font = new Font("Segoe UI", 10);
        Padding = new Padding(28, 24, 28, 20);
        ThemeUi.EnableBufferedPaint(this);

        var header = new Panel { Dock = DockStyle.Top, Height = 88 };
        var heading = new Label
        {
            Text = "Writing Statistics",
            Font = new Font("Segoe UI", 18, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(4, 4)
        };
        var intro = new Label
        {
            Text = "Totals LexFlow has helped with — opened from Settings whenever you want to look.",
            AutoSize = true,
            MaximumSize = new Size(900, 0),
            Location = new Point(4, 44)
        };
        var buttons = new Panel { Dock = DockStyle.Bottom, Height = 52 };
        var refresh = new Button
        {
            Text = "Refresh",
            Size = new Size(110, 36),
            Location = new Point(4, 8)
        };
        refresh.Click += (_, _) => RefreshStats();
        var close = new Button
        {
            Text = "Close",
            Size = new Size(110, 36),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        close.Click += (_, _) => Close();
        buttons.Controls.Add(refresh);
        buttons.Controls.Add(close);
        buttons.Resize += (_, _) => close.Left = Math.Max(130, buttons.ClientSize.Width - close.Width - 4);

        _unavailable.Text = "Statistics are available only while LexFlow is running.";
        _unavailable.Dock = DockStyle.Fill;
        _unavailable.Padding = new Padding(4, 12, 4, 0);
        _unavailable.Visible = false;

        _content.Dock = DockStyle.Fill;
        _content.Padding = new Padding(0, 8, 0, 8);

        Controls.Add(_content);
        Controls.Add(_unavailable);
        Controls.Add(buttons);
        header.Controls.Add(heading);
        header.Controls.Add(intro);
        Controls.Add(header);

        var grid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 28));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 42));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 30));
        _content.Controls.Add(grid);

        var suggestions = CreateGroup("Suggestions");
        var suggestionTable = MetricTable(
            ("Suggestions you've accepted", _accepted));
        suggestionTable.Dock = DockStyle.Fill;
        suggestions.Controls.Add(suggestionTable);
        grid.Controls.Add(suggestions, 0, 0);

        var expansions = CreateGroup("Text Expansions");
        var expansionLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 8, 8, 8)
        };
        expansionLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
        expansionLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var usesTable = MetricTable(("Times used", _expansionUses));
        usesTable.Dock = DockStyle.Fill;
        expansionLayout.Controls.Add(usesTable, 0, 0);

        _topExpansions.View = View.Details;
        _topExpansions.FullRowSelect = true;
        _topExpansions.HeaderStyle = ColumnHeaderStyle.Nonclickable;
        _topExpansions.BorderStyle = BorderStyle.FixedSingle;
        _topExpansions.Dock = DockStyle.Fill;
        _topExpansions.Font = new Font("Segoe UI", 10);
        _topExpansions.Columns.Add("Trigger", 620);
        _topExpansions.Columns.Add("Uses", 160, HorizontalAlignment.Right);
        _topExpansions.Resize += (_, _) =>
        {
            if (_topExpansions.Columns.Count < 2)
            {
                return;
            }

            var usesWidth = 160;
            _topExpansions.Columns[1].Width = usesWidth;
            _topExpansions.Columns[0].Width = Math.Max(200, _topExpansions.ClientSize.Width - usesWidth - 8);
        };
        expansionLayout.Controls.Add(_topExpansions, 0, 1);
        expansions.Controls.Add(expansionLayout);
        grid.Controls.Add(expansions, 0, 1);

        var productivity = CreateGroup("Productivity");
        var productivityLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8, 8, 8, 8)
        };
        productivityLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        productivityLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var prodTable = MetricTable(
            ("Keystrokes you've saved", _characters),
            ("Estimated time saved", _timeSaved));
        prodTable.Dock = DockStyle.Fill;
        productivityLayout.Controls.Add(prodTable, 0, 0);
        var footnote = new Label
        {
            Text = "Time saved is estimated at 200 milliseconds per inserted character.",
            AutoSize = true,
            Dock = DockStyle.Top,
            Padding = new Padding(4, 4, 4, 8),
            ForeColor = SystemColors.GrayText
        };
        productivityLayout.Controls.Add(footnote, 0, 1);
        productivity.Controls.Add(productivityLayout);
        grid.Controls.Add(productivity, 0, 2);
    }

    private static GroupBox CreateGroup(string title) => new()
    {
        Text = title,
        Dock = DockStyle.Fill,
        Padding = new Padding(12, 10, 12, 12),
        Margin = new Padding(0, 0, 0, 12)
    };

    private static TableLayoutPanel MetricTable(params (string Caption, Label Value)[] rows)
    {
        var table = new TableLayoutPanel
        {
            ColumnCount = 2,
            RowCount = rows.Length,
            Padding = new Padding(8, 8, 8, 8)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 62));
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 38));

        for (var i = 0; i < rows.Length; i++)
        {
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / rows.Length));
            var caption = new Label
            {
                Text = rows[i].Caption,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Font = new Font("Segoe UI", 10)
            };
            rows[i].Value.Dock = DockStyle.Fill;
            table.Controls.Add(caption, 0, i);
            table.Controls.Add(rows[i].Value, 1, i);
        }

        return table;
    }

    private static Label ValueLabel() => new()
    {
        Text = "—",
        TextAlign = ContentAlignment.MiddleRight,
        Font = new Font("Segoe UI", 11, FontStyle.Bold)
    };

    private void RefreshStats()
    {
        var running = _personalization != null && _expansions != null;
        _unavailable.Visible = !running;
        _content.Visible = running;
        if (!running)
        {
            return;
        }

        var stats = WritingStatsAggregator.Build(_personalization!, _expansions!, null);
        _accepted.Text = stats.SuggestionsAccepted.ToString("N0");
        _expansionUses.Text = stats.ExpansionUses.ToString("N0");
        _characters.Text = stats.KeystrokesSaved.ToString("N0");
        _timeSaved.Text = FormatDuration(stats.EstimatedSecondsSaved);

        _topExpansions.BeginUpdate();
        _topExpansions.Items.Clear();
        if (stats.TopExpansions.Count == 0)
        {
            _topExpansions.Items.Add(new ListViewItem(["No expansions recorded", ""]));
        }
        else
        {
            foreach (var (trigger, uses) in stats.TopExpansions)
            {
                _topExpansions.Items.Add(new ListViewItem([trigger, uses.ToString("N0")]));
            }
        }
        _topExpansions.EndUpdate();
    }

    private static string FormatDuration(double seconds)
    {
        if (seconds < 60)
        {
            return $"{seconds:0.0} seconds";
        }

        var minutes = (int)(seconds / 60);
        var remainder = seconds - (minutes * 60);
        return remainder < 0.5
            ? $"{minutes} minutes"
            : $"{minutes} minutes {remainder:0} seconds";
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
