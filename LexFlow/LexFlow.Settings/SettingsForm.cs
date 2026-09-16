using LexFlow.AI;
using LexFlow.AI.Interfaces;
using LexFlow.Core.Interfaces;
using LexFlow.Storage;
using LexFlow.Profiles;
using LexFlow.Privacy;
using LexFlow.Core.Theming;
using LexFlow.Core.Pipeline;
using LexFlow.Core.Learning;
using LexFlow.Core.Expansion;
using LexFlow.Core;
using LexFlow.Core.Models;
using LexFlow.Overlay.Interfaces;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace LexFlow.Settings;

public partial class SettingsForm : Form
{
    private readonly IStorage _storage;
    private readonly Profile _profile;
    private readonly PrivacyGuard _privacyGuard;
    private readonly ThemeManager? _themeManager;
    private readonly SuggestionPipeline? _suggestionPipeline;
    private readonly ISuggestionOverlay? _suggestionOverlay;
    private readonly PersonalizationManager? _personalization;
    private readonly TextExpansionManager? _expansions;
    private readonly IEditConfirmation? _editConfirmation;
    private readonly CloudAiActivityLog? _cloudAiLog;
    private readonly bool _ownsProfile;

    private CheckBox _chkAutoStart = null!;
    private CheckBox _chkMinimizeToTray = null!;
    private CheckBox _chkEnableSounds = null!;
    private CheckBox _chkCheckUpdates = null!;
    private Label _lblQuickToggle = null!;
    private ComboBox _cmbAIProvider = null!;
    private TextBox _txtAPIKey = null!;
    private Label _lblAiRecommended = null!;
    private LinkLabel _lnkMoreProviders = null!;
    private Label _lblAiStatus = null!;
    private Button _btnGetApiKey = null!;
    private Button _btnCancelWait = null!;
    private ComboBox _cmbAiModel = null!;
    private Label _lblAiModel = null!;
    private CheckBox _chkEnableRewriteHotkey = null!;
    private CheckBox _chkEnableGrammarHotkey = null!;
    private CheckBox _chkGrammarChecking = null!;
    private CheckBox _chkLocalMode = null!;
    private TextBox _txtBlockedApps = null!;
    private Button _btnAddBlockedApp = null!;
    private ComboBox _cmbTheme = null!;
    private ComboBox _cmbSuggestionSort = null!;
    private ComboBox _cmbSuggestionPlacement = null!;
    private CheckBox _chkRequireConfirmation = null!;
    private TextBox _txtAppTone = null!;
    private ComboBox _cmbAppToneCategory = null!;
    private ListBox _lstAppTone = null!;
    private Button _btnWritingStats = null!;
    private Button _btnLearnedWords = null!;
    private Label _lblStyleSummary = null!;
    private Button _btnResetStyle = null!;
    private ListBox _lstAdaptations = null!;
    private Button _btnUndoAdaptation = null!;
    private ComboBox _cmbGrammarSensitivity = null!;
    private CheckBox _chkMuteCasualGrammar = null!;
    private TextBox _txtGrammarMutedApps = null!;
    private Button _btnExportLearning = null!;
    private Button _btnImportLearning = null!;
    private Button _btnAiLog = null!;
    private FlowLayoutPanel _compactHost = null!;
    private TableLayoutPanel _wideHost = null!;
    private FlowLayoutPanel _colLeft = null!;
    private FlowLayoutPanel _colMiddle = null!;
    private FlowLayoutPanel _colRight = null!;
    private FlowLayoutPanel _sectionGeneral = null!;
    private FlowLayoutPanel _sectionAi = null!;
    private FlowLayoutPanel _sectionPrivacy = null!;
    private FlowLayoutPanel _sectionAppearance = null!;
    private FlowLayoutPanel _sectionAppTone = null!;
    private FlowLayoutPanel _sectionWriting = null!;
    private FlowLayoutPanel _blockedRow = null!;
    private FlowLayoutPanel _toneRow = null!;
    private bool _wideLayoutActive;
    private FormClosingEventHandler? _minimizeToTrayHandler;
    private readonly System.Windows.Forms.Timer _persistTimer;
    private readonly System.Windows.Forms.Timer _aiProbeTimer;
    private readonly System.Windows.Forms.Timer _clipboardWatchTimeout;
    private readonly Action<IAIProvider?>? _applyAiProvider;
    private CancellationTokenSource? _aiProbeCts;
    private bool _loading;
    private bool _aiAdvancedVisible;
    private bool _watchingClipboard;
    private string? _waitingProvider;
    private string _activeProvider = "None";
    private string _activeApiKey = string.Empty;
    private bool _aiValidated;

    public SettingsForm()
        : this(null, null, null)
    {
    }

    public SettingsForm(Profile? profile, PrivacyGuard? privacyGuard, IStorage? storage, ThemeManager? themeManager = null, SuggestionPipeline? suggestionPipeline = null, ISuggestionOverlay? suggestionOverlay = null, PersonalizationManager? personalization = null, TextExpansionManager? expansions = null, IEditConfirmation? editConfirmation = null, Action<IAIProvider?>? applyAiProvider = null, CloudAiActivityLog? cloudAiLog = null)
    {
        _ownsProfile = profile == null;
        var storagePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "LexFlow");
        _storage = storage ?? new EncryptedStorage(storagePath);
        _profile = profile ?? new Profile(_storage) { Id = Profile.DefaultProfileId };
        _privacyGuard = privacyGuard ?? new PrivacyGuard();
        _themeManager = themeManager;
        _suggestionPipeline = suggestionPipeline;
        _suggestionOverlay = suggestionOverlay;
        _personalization = personalization;
        _expansions = expansions;
        _editConfirmation = editConfirmation;
        _applyAiProvider = applyAiProvider;
        _cloudAiLog = cloudAiLog;
        _persistTimer = new System.Windows.Forms.Timer { Interval = 400 };
        _persistTimer.Tick += (_, _) => FlushPersist();
        _aiProbeTimer = new System.Windows.Forms.Timer { Interval = 500 };
        _aiProbeTimer.Tick += (_, _) =>
        {
            _aiProbeTimer.Stop();
            _ = ProbeAiAsync();
        };
        _clipboardWatchTimeout = new System.Windows.Forms.Timer { Interval = 3 * 60 * 1000 };
        _clipboardWatchTimeout.Tick += (_, _) => StopClipboardWatch("Timed out waiting for a key. Paste it here if you already copied it.");

        if (_ownsProfile)
        {
            _profile.LoadAsync().GetAwaiter().GetResult();
        }

        InitializeComponent();
        LoadSettings();
        HookAutoApply();
        SetupMinimizeToTrayBehavior();
        Shown += OnSettingsShown;
        Resize += (_, _) => UpdateLayoutMode();
        FormClosing += (_, _) =>
        {
            StopClipboardWatch();
            FlushPersist();
        };
        FormClosed += (_, _) =>
        {
            if (_themeManager != null)
            {
                _themeManager.ThemeChanged -= OnExternalThemeChanged;
            }

            StopClipboardWatch();
            _persistTimer.Stop();
            _persistTimer.Dispose();
            _aiProbeTimer.Stop();
            _aiProbeTimer.Dispose();
            _clipboardWatchTimeout.Stop();
            _clipboardWatchTimeout.Dispose();
            _aiProbeCts?.Cancel();
            _aiProbeCts?.Dispose();
        };
        VisibleChanged += (_, _) =>
        {
            if (!Visible)
            {
                StopClipboardWatch();
            }
        };
        ApplyTheme();
        if (_themeManager != null)
        {
            _themeManager.ThemeChanged += OnExternalThemeChanged;
        }
    }

    private void InitializeComponent()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        Text = "LexFlow Settings";
        ClientSize = new Size(700, 800);
        MinimumSize = new Size(680, 600);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        WindowState = FormWindowState.Normal;
        ShowInTaskbar = true;
        Font = new Font("Segoe UI", 9);
        Padding = new Padding(24, 20, 24, 16);
        ThemeUi.EnableBufferedPaint(this);

        var footer = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Padding = new Padding(8, 8, 8, 0)
        };
        var btnClose = new Button { Text = "Close", AutoSize = true, Padding = new Padding(16, 6, 16, 6), Margin = new Padding(0, 0, 8, 0) };
        btnClose.Click += (_, _) => Close();
        var btnAbout = new Button { Text = "About", AutoSize = true, Padding = new Padding(16, 6, 16, 6) };
        btnAbout.Click += OnAboutClicked;
        footer.Controls.Add(btnClose);
        footer.Controls.Add(btnAbout);
        Controls.Add(footer);

        _compactHost = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(8)
        };
        ThemeUi.EnableBufferedPaint(_compactHost);

        _colLeft = ColumnHost();
        _colMiddle = ColumnHost();
        _colRight = ColumnHost();
        _wideHost = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Visible = false,
            Padding = new Padding(8),
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };
        _wideHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        _wideHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.3f));
        _wideHost.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.4f));
        _wideHost.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        _wideHost.Controls.Add(_colLeft, 0, 0);
        _wideHost.Controls.Add(_colMiddle, 1, 0);
        _wideHost.Controls.Add(_colRight, 2, 0);
        ThemeUi.EnableBufferedPaint(_wideHost);

        _chkAutoStart = Check("Start with Windows");
        _chkMinimizeToTray = Check("Minimize to system tray");
        _chkEnableSounds = Check("Enable sound feedback");
        _chkCheckUpdates = Check("Check for updates on startup (asks before installing)");
        _lblQuickToggle = Caption("Pause everything: double-press Ctrl. Same as Enable/Disable on the tray icon.");
        _sectionGeneral = Section("General", _chkAutoStart, _chkMinimizeToTray, _chkEnableSounds, _chkCheckUpdates, _lblQuickToggle);

        _lblAiRecommended = Caption("OpenAI (recommended) — paste an API key to connect.");
        _lnkMoreProviders = new LinkLabel
        {
            Text = "More providers",
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        _lnkMoreProviders.LinkClicked += (_, _) => ShowAdvancedProviders();
        _cmbAIProvider = Combo(360);
        _cmbAIProvider.Items.AddRange(AiProviderCatalog.AllProviders.Cast<object>().ToArray());
        _cmbAIProvider.SelectedIndex = 0;
        _cmbAIProvider.Visible = false;
        _txtAPIKey = Field(360, "Paste API key", isPassword: true);
        _btnGetApiKey = new Button
        {
            Text = "Get your API key",
            AutoSize = true,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(0, 0, 8, 8)
        };
        _btnGetApiKey.Click += (_, _) => OnGetApiKeyClicked();
        _btnCancelWait = new Button
        {
            Text = "Cancel",
            AutoSize = true,
            Padding = new Padding(10, 4, 10, 4),
            Margin = new Padding(0, 0, 0, 8),
            Visible = false
        };
        _btnCancelWait.Click += (_, _) => StopClipboardWatch("Cancelled. You can still paste a key.");
        _lblAiStatus = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(360, 0),
            Margin = new Padding(0, 0, 0, 8),
            ForeColor = Color.FromArgb(90, 90, 90),
            Text = "Paste a key to connect. LexFlow will check it automatically."
        };
        _lblAiModel = Caption("Model (set automatically; change only if you want a different one)");
        _cmbAiModel = Combo(360);
        _sectionAi = Section("AI", _lblAiRecommended, _lnkMoreProviders, _cmbAIProvider, _btnGetApiKey, _txtAPIKey, _btnCancelWait, _lblAiStatus, _lblAiModel, _cmbAiModel);

        _chkLocalMode = Check("Local-only mode (no cloud)");
        _blockedRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 4, 0, 8)
        };
        _txtBlockedApps = Field(240, "Blocked apps (comma-separated)");
        _txtBlockedApps.Margin = new Padding(0, 4, 8, 0);
        _btnAddBlockedApp = new Button
        {
            Text = "Add running app…",
            AutoSize = true,
            Padding = new Padding(10, 4, 10, 4),
            Margin = Padding.Empty
        };
        _btnAddBlockedApp.Click += OnAddBlockedAppClicked;
        _blockedRow.Controls.Add(_txtBlockedApps);
        _blockedRow.Controls.Add(_btnAddBlockedApp);
        _btnAiLog = new Button
        {
            Text = "View cloud AI activity log…",
            AutoSize = true,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 4, 0, 8)
        };
        _btnAiLog.Click += OnAiLogClicked;
        _sectionPrivacy = Section(
            "Privacy",
            _chkLocalMode,
            Caption("Blocked apps and password fields are skipped for suggestions, rewrite, and grammar."),
            _blockedRow,
            _btnAiLog);

        _cmbTheme = Combo(360);
        _cmbTheme.Items.AddRange(new[] { "Light", "Dark", "High Contrast" });
        _cmbTheme.SelectedIndex = 0;
        _cmbSuggestionSort = Combo(360);
        _cmbSuggestionSort.Items.AddRange(new[] { "Most Relevant", "Most Used" });
        _cmbSuggestionSort.SelectedIndex = 0;
        _cmbSuggestionPlacement = Combo(360);
        _cmbSuggestionPlacement.Items.AddRange(new[] { "Below the word", "Above the word" });
        _cmbSuggestionPlacement.SelectedIndex = 0;
        _chkRequireConfirmation = Check("Preview AI rewrites before applying");
        _sectionAppearance = Section(
            "Appearance",
            Caption("Theme"),
            _cmbTheme,
            Caption("Suggestion order"),
            _cmbSuggestionSort,
            Caption("Suggestion position"),
            _cmbSuggestionPlacement,
            _chkRequireConfirmation);

        _lstAppTone = new ListBox { Width = 360, Height = 110, Margin = new Padding(0, 0, 0, 8) };
        _toneRow = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = true,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        _txtAppTone = Field(140, "Name or name.exe");
        _txtAppTone.Margin = new Padding(0, 0, 8, 0);
        _cmbAppToneCategory = Combo(120);
        _cmbAppToneCategory.Items.AddRange(new object[] { "Casual", "Formal", "Code", "Neutral" });
        _cmbAppToneCategory.SelectedIndex = 0;
        var btnAddTone = new Button { Text = "Add", AutoSize = true, Margin = new Padding(8, 0, 8, 0) };
        btnAddTone.Click += (_, _) => AddAppToneOverride();
        var btnRemoveTone = new Button { Text = "Remove", AutoSize = true };
        btnRemoveTone.Click += (_, _) => RemoveAppToneOverride();
        _toneRow.Controls.Add(_txtAppTone);
        _toneRow.Controls.Add(_cmbAppToneCategory);
        _toneRow.Controls.Add(btnAddTone);
        _toneRow.Controls.Add(btnRemoveTone);
        _sectionAppTone = Section(
            "App tone",
            Caption("Built-in defaults cover Slack, Teams, Outlook, Word, and editors. Add a row only to override an app."),
            _lstAppTone,
            _toneRow);

        _btnWritingStats = new Button
        {
            Text = "View writing stats…",
            AutoSize = true,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 4, 0, 8)
        };
        _btnWritingStats.Click += OnWritingStatsClicked;
        _btnLearnedWords = new Button
        {
            Text = "Manage learned words…",
            AutoSize = true,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 4, 0, 16)
        };
        _btnLearnedWords.Click += OnLearnedWordsClicked;
        _btnExportLearning = new Button
        {
            Text = "Export learned data…",
            AutoSize = true,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 4, 8, 8)
        };
        _btnExportLearning.Click += OnExportLearningClicked;
        _btnImportLearning = new Button
        {
            Text = "Import learned data…",
            AutoSize = true,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 4, 0, 8)
        };
        _btnImportLearning.Click += OnImportLearningClicked;
        _lblStyleSummary = new Label { AutoSize = true, MaximumSize = new Size(360, 0), Margin = new Padding(0, 4, 0, 8) };
        _btnResetStyle = new Button
        {
            Text = "Reset writing style",
            AutoSize = true,
            Padding = new Padding(12, 6, 12, 6),
            Margin = new Padding(0, 0, 0, 8)
        };
        _btnResetStyle.Click += OnResetStyleClicked;
        _lstAdaptations = new ListBox { Width = 360, Height = 72, Margin = new Padding(0, 0, 0, 8) };
        _btnUndoAdaptation = new Button { Text = "Undo selected adjustment", AutoSize = true, Margin = new Padding(0, 0, 0, 8) };
        _btnUndoAdaptation.Click += OnUndoAdaptationClicked;
        _chkEnableRewriteHotkey = Check("Optional shortcut: Ctrl+Alt+R for rewrite (also: select text, then click Aa)");
        _chkGrammarChecking = Check("Automatically suggest grammar fixes (Tab to accept — no shortcut needed)");
        _chkEnableGrammarHotkey = Check("Optional shortcut: Ctrl+Alt+G to check now");
        _cmbGrammarSensitivity = Combo(360);
        _cmbGrammarSensitivity.Items.AddRange(new[] { "Low", "Medium", "High" });
        _cmbGrammarSensitivity.SelectedIndex = 1;
        _chkMuteCasualGrammar = Check("Mute grammar checks in casual apps");
        _txtGrammarMutedApps = Field(360, "Muted grammar apps (comma-separated)");
        _sectionWriting = Section(
            "Writing",
            _btnWritingStats,
            _btnLearnedWords,
            _btnExportLearning,
            _btnImportLearning,
            Caption("Detected writing style"),
            _lblStyleSummary,
            _btnResetStyle,
            Caption("Style adjustments (from repeated rejections)"),
            _lstAdaptations,
            _btnUndoAdaptation,
            Caption("Grammar (local rules: agreement, typos, punctuation)"),
            _chkGrammarChecking,
            _cmbGrammarSensitivity,
            _chkMuteCasualGrammar,
            _txtGrammarMutedApps,
            _chkEnableRewriteHotkey,
            _chkEnableGrammarHotkey);

        Controls.Add(_wideHost);
        Controls.Add(_compactHost);
        ApplyCompactLayout();
    }

    private static FlowLayoutPanel ColumnHost() => new()
    {
        Dock = DockStyle.Fill,
        FlowDirection = FlowDirection.TopDown,
        WrapContents = false,
        AutoScroll = true,
        Padding = new Padding(12, 8, 12, 8)
    };

    private static FlowLayoutPanel Section(string title, params Control[] children)
    {
        var section = new FlowLayoutPanel
        {
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        var heading = Heading(title);
        if (title == "General")
        {
            heading.Margin = new Padding(0, 0, 0, 8);
        }

        section.Controls.Add(heading);
        foreach (var child in children)
        {
            section.Controls.Add(child);
        }

        return section;
    }

    private static Label Heading(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Font = new Font("Segoe UI", 10, FontStyle.Bold),
        Margin = new Padding(0, 16, 0, 8)
    };

    private static Label Caption(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(0, 4, 0, 4)
    };

    private static CheckBox Check(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Margin = new Padding(0, 2, 0, 6)
    };

    private static ComboBox Combo(int width)
    {
        var combo = new ComboBox
        {
            Width = width,
            DropDownStyle = ComboBoxStyle.DropDownList,
            Margin = new Padding(0, 0, 0, 8)
        };
        ThemeUi.AttachComboDrawing(combo);

        // AttachComboDrawing switches the box to OwnerDrawFixed, so every repaint of
        // the closed box and each list item is custom-painted. That flickers without
        // double buffering.
        ThemeUi.EnableBufferedPaint(combo);
        return combo;
    }

    private static TextBox Field(int width, string placeholder, bool isPassword = false) => new()
    {
        Width = width,
        PlaceholderText = placeholder,
        UseSystemPasswordChar = isPassword,
        Margin = new Padding(0, 0, 0, 8)
    };

    private void OnSettingsShown(object? sender, EventArgs e)
    {
        UpdateLayoutMode();
        Activate();
        BringToFront();
    }

    private void UpdateLayoutMode()
    {
        if (_compactHost == null || _sectionGeneral == null)
        {
            return;
        }

        var wide = WindowState == FormWindowState.Maximized;
        if (wide == _wideLayoutActive && IsHandleCreated)
        {
            ApplyFieldSizes();
            return;
        }

        if (wide)
        {
            ApplyWideLayout();
        }
        else
        {
            ApplyCompactLayout();
        }
    }

    private void ApplyCompactLayout()
    {
        SuspendLayout();
        _wideHost.Visible = false;
        _compactHost.Visible = true;
        PlaceSections(_compactHost, _sectionGeneral, _sectionAi, _sectionPrivacy, _sectionAppearance, _sectionAppTone, _sectionWriting);
        _wideLayoutActive = false;
        Padding = new Padding(24, 20, 24, 16);
        ResumeLayout(true);
        ApplyFieldSizes();
        ApplyTheme();
    }

    private void ApplyWideLayout()
    {
        SuspendLayout();
        _compactHost.Visible = false;
        _wideHost.Visible = true;
        PlaceSections(_colLeft, _sectionGeneral, _sectionAi, _sectionPrivacy);
        PlaceSections(_colMiddle, _sectionAppearance, _sectionWriting);
        PlaceSections(_colRight, _sectionAppTone);
        _wideLayoutActive = true;
        Padding = new Padding(28, 24, 28, 20);
        ResumeLayout(true);
        ApplyFieldSizes();
        ApplyTheme();
    }

    private static void PlaceSections(Control parent, params Control[] sections)
    {
        parent.SuspendLayout();
        foreach (var section in sections)
        {
            section.Parent = null;
        }

        parent.Controls.Clear();
        foreach (var section in sections)
        {
            parent.Controls.Add(section);
        }

        parent.ResumeLayout(true);
    }

    private void ApplyFieldSizes()
    {
        var host = _wideLayoutActive ? _colRight : _compactHost;
        var width = Math.Max(280, host.ClientSize.Width - 36);
        if (_wideLayoutActive)
        {
            var colWidth = Math.Max(280, _colLeft.ClientSize.Width - 36);
            _cmbAIProvider.Width = colWidth;
            _txtAPIKey.Width = colWidth;
            _cmbAiModel.Width = colWidth;
            _lblAiRecommended.MaximumSize = new Size(colWidth, 0);
            _lblAiStatus.MaximumSize = new Size(colWidth, 0);
            _txtBlockedApps.Width = Math.Max(160, colWidth - 160);
            _cmbTheme.Width = Math.Max(280, _colMiddle.ClientSize.Width - 36);
            _cmbSuggestionSort.Width = _cmbTheme.Width;
            _cmbSuggestionPlacement.Width = _cmbTheme.Width;
            _lstAppTone.Width = width;
            _lstAppTone.Height = Math.Max(220, ClientSize.Height - 360);
            _lstAdaptations.Width = Math.Max(280, _colMiddle.ClientSize.Width - 36);
            _cmbGrammarSensitivity.Width = _lstAdaptations.Width;
            _txtGrammarMutedApps.Width = _lstAdaptations.Width;
            return;
        }

        _cmbAIProvider.Width = 360;
        _txtAPIKey.Width = 360;
        _cmbAiModel.Width = 360;
        _lblAiRecommended.MaximumSize = new Size(360, 0);
        _lblAiStatus.MaximumSize = new Size(360, 0);
        _txtBlockedApps.Width = 240;
        _cmbTheme.Width = 360;
        _cmbSuggestionSort.Width = 360;
        _cmbSuggestionPlacement.Width = 360;
        _lstAppTone.Width = 360;
        _lstAppTone.Height = 110;
    }

    private void OnWritingStatsClicked(object? sender, EventArgs e)
    {
        using var form = new WritingStatsForm(_personalization, _expansions, _themeManager);
        form.ShowDialog(this);
    }

    private void OnExportLearningClicked(object? sender, EventArgs e)
    {
        if (_personalization == null)
        {
            MessageBox.Show(this, "Learned data is available while LexFlow is running.", "Export");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "LexFlow learning (*.json)|*.json",
            FileName = "lexflow-learning.json"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        File.WriteAllText(dialog.FileName, _personalization.ExportLearningData());
    }

    private void OnImportLearningClicked(object? sender, EventArgs e)
    {
        if (_personalization == null)
        {
            MessageBox.Show(this, "Learned data is available while LexFlow is running.", "Import");
            return;
        }

        using var dialog = new OpenFileDialog { Filter = "LexFlow learning (*.json)|*.json" };
        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        var json = File.ReadAllText(dialog.FileName);
        if (_personalization.ImportLearningData(json))
        {
            RefreshLearnedUi();
            MessageBox.Show(this, "Imported learned vocabulary and writing style.", "Import");
        }
        else
        {
            MessageBox.Show(this, "That file is not a valid LexFlow learning export.", "Import");
        }
    }

    private void OnAiLogClicked(object? sender, EventArgs e)
    {
        using var form = new CloudAiActivityForm(_cloudAiLog);
        form.ShowDialog(this);
    }

    private void OnLearnedWordsClicked(object? sender, EventArgs e)
    {
        using var form = new LearnedWordsForm(_suggestionPipeline, _themeManager);
        form.ShowDialog(this);
    }

    private void RefreshLearnedUi()
    {
        _lblStyleSummary.Text = _personalization?.GetStyleSummary() ?? "Available while LexFlow is running.";
        _lstAdaptations.Items.Clear();
        if (_personalization == null)
        {
            return;
        }

        foreach (var record in _personalization.GetAdaptations().Where(a => !a.Undone))
        {
            _lstAdaptations.Items.Add(record);
        }
    }

    private void OnResetStyleClicked(object? sender, EventArgs e)
    {
        if (_personalization == null)
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            "Clear the learned writing-style profile only? Vocabulary and other personalization are left unchanged.",
            "Reset writing style",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (result != DialogResult.Yes)
        {
            return;
        }

        _personalization.ResetWritingStyle();
        RefreshLearnedUi();
    }

    private void OnUndoAdaptationClicked(object? sender, EventArgs e)
    {
        if (_personalization == null || _lstAdaptations.SelectedItem is not AdaptationRecord record)
        {
            return;
        }

        _personalization.UndoAdaptation(record.Id);
        RefreshLearnedUi();
    }

    private void SetupMinimizeToTrayBehavior()
    {
        if (_minimizeToTrayHandler != null)
        {
            FormClosing -= _minimizeToTrayHandler;
            _minimizeToTrayHandler = null;
        }

        if (_chkMinimizeToTray.Checked)
        {
            _minimizeToTrayHandler = (s, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing)
                {
                    e.Cancel = true;
                    Hide();
                }
            };
            FormClosing += _minimizeToTrayHandler;
        }
    }

    private void HookAutoApply()
    {
        _chkAutoStart.CheckedChanged += (_, _) => ApplyNow();
        _chkMinimizeToTray.CheckedChanged += (_, _) => ApplyNow();
        _chkEnableSounds.CheckedChanged += (_, _) => ApplyNow();
        _chkCheckUpdates.CheckedChanged += (_, _) => ApplyNow();
        _chkLocalMode.CheckedChanged += (_, _) =>
        {
            ApplyNow();
            UpdateAiEntryMode();
            ScheduleAiProbe();
        };
        _cmbAIProvider.SelectedIndexChanged += (_, _) =>
        {
            if (_loading)
            {
                return;
            }

            UpdateAiEntryMode();
            StopClipboardWatch();
            ScheduleAiProbe();
        };
        _cmbAiModel.SelectedIndexChanged += (_, _) => ApplyNow();
        _chkEnableRewriteHotkey.CheckedChanged += (_, _) => ApplyNow();
        _chkGrammarChecking.CheckedChanged += (_, _) => ApplyNow();
        _chkEnableGrammarHotkey.CheckedChanged += (_, _) => ApplyNow();
        _cmbSuggestionSort.SelectedIndexChanged += (_, _) => ApplyNow();
        _cmbSuggestionPlacement.SelectedIndexChanged += (_, _) => ApplyNow();
        _chkRequireConfirmation.CheckedChanged += (_, _) => ApplyNow();
        _cmbGrammarSensitivity.SelectedIndexChanged += (_, _) => ApplyNow();
        _chkMuteCasualGrammar.CheckedChanged += (_, _) => ApplyNow();
        _cmbTheme.SelectedIndexChanged += OnThemeChanged;
        _txtAPIKey.TextChanged += (_, _) =>
        {
            SchedulePersist();
            ScheduleAiProbe();
        };
        _txtAPIKey.Leave += (_, _) =>
        {
            _aiProbeTimer.Stop();
            _ = ProbeAiAsync();
        };
        _txtBlockedApps.TextChanged += (_, _) => SchedulePersist();
        _txtGrammarMutedApps.TextChanged += (_, _) => SchedulePersist();
    }

    private void LoadSettings()
    {
        _loading = true;
        _chkAutoStart.Checked = WindowsStartup.IsEnabled();
        _chkMinimizeToTray.Checked = _profile.GetSetting("MinimizeToTray", true);
        _chkEnableSounds.Checked = _profile.GetSetting("EnableSounds", true);
        _chkCheckUpdates.Checked = _profile.GetSetting("EnableAutoUpdates", true);

        var provider = _profile.GetSetting("AIProvider", "None");
        _activeProvider = string.IsNullOrWhiteSpace(provider) ? "None" : provider;
        _activeApiKey = _profile.GetSetting("APIKey", string.Empty);
        _aiValidated = _profile.GetSetting("AIKeyValidated", !string.IsNullOrEmpty(_activeApiKey) || _activeProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase));
        var providerIndex = _cmbAIProvider.Items.IndexOf(_activeProvider);
        _cmbAIProvider.SelectedIndex = providerIndex >= 0 ? providerIndex : _cmbAIProvider.Items.IndexOf(AiProviderCatalog.Recommended);
        _txtAPIKey.Text = _activeApiKey;
        FillModelChoices(_activeProvider.Equals("None", StringComparison.OrdinalIgnoreCase) ? AiProviderCatalog.Recommended : _activeProvider, _profile.GetSetting("AIModel", string.Empty));
        if (AiProviderCatalog.ShowAdvancedByDefault(_activeProvider))
        {
            ShowAdvancedProviders();
        }

        UpdateAiEntryMode();
        SetAiStatus(
            _aiValidated && !_activeProvider.Equals("None", StringComparison.OrdinalIgnoreCase)
                ? (_activeProvider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
                    ? "Ollama is the saved provider. Checking…"
                    : "Key saved. Checking…")
                : "Paste a key to connect. LexFlow will check it automatically.",
            _aiValidated ? Color.FromArgb(0, 120, 80) : Color.FromArgb(90, 90, 90));
        _chkLocalMode.Checked = _profile.GetSetting("LocalMode", false);

        var blockedApps = _profile.GetSetting<List<string>>("BlockedApplications", new List<string>());
        _txtBlockedApps.Text = string.Join(", ", blockedApps);

        var currentTheme = _profile.GetSetting("Theme", "Light");
        var themeIndex = _cmbTheme.Items.IndexOf(currentTheme);
        _cmbTheme.SelectedIndex = themeIndex >= 0 ? themeIndex : 0;

        var sortMode = _profile.GetSetting("SuggestionSortMode", "Relevant");
        _cmbSuggestionSort.SelectedIndex = string.Equals(sortMode, "Used", StringComparison.OrdinalIgnoreCase) ? 1 : 0;

        var placement = _profile.GetSetting("SuggestionPlacement", "Below");
        _cmbSuggestionPlacement.SelectedIndex = string.Equals(placement, "Above", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _chkRequireConfirmation.Checked = _profile.GetSetting("RequireConfirmationForEdits", true);
        var grammarSensitivity = _profile.GetSetting("GrammarSensitivity", "Medium");
        var grammarIndex = _cmbGrammarSensitivity.Items.IndexOf(grammarSensitivity);
        _cmbGrammarSensitivity.SelectedIndex = grammarIndex >= 0 ? grammarIndex : 1;
        _chkMuteCasualGrammar.Checked = _profile.GetSetting("MuteGrammarForCasualApps", false);
        _chkEnableRewriteHotkey.Checked = _profile.GetSetting("EnableRewriteHotkey", true);
        _chkGrammarChecking.Checked = _profile.GetSetting("GrammarChecking", true);
        _chkEnableGrammarHotkey.Checked = _profile.GetSetting("EnableGrammarHotkey", true);
        var mutedGrammar = _profile.GetSetting<List<string>>("GrammarMutedApps", new List<string>());
        _txtGrammarMutedApps.Text = string.Join(", ", mutedGrammar);
        _lstAppTone.Items.Clear();
        foreach (var row in _profile.GetSetting<List<string>>("AppCategoryOverrides", new List<string>()))
        {
            _lstAppTone.Items.Add(row);
        }

        RefreshLearnedUi();
        _loading = false;
        if (!_chkLocalMode.Checked)
        {
            ScheduleAiProbe();
        }
    }

    private void ApplyNow()
    {
        if (_loading)
        {
            return;
        }

        ApplySettings();
        SchedulePersist();
    }

    private void SchedulePersist()
    {
        if (_loading)
        {
            return;
        }

        _persistTimer.Stop();
        _persistTimer.Start();
    }

    private void FlushPersist()
    {
        _persistTimer.Stop();
        if (_loading)
        {
            return;
        }

        ApplySettings();
        _ = _profile.SaveAsync();
    }

    private void ApplySettings()
    {
        var blockedApps = _txtBlockedApps.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        WindowsStartup.SetEnabled(_chkAutoStart.Checked);

        var selectedTheme = _cmbTheme.SelectedItem?.ToString() ?? "Light";
        var selectedSortMode = _cmbSuggestionSort.SelectedIndex == 1 ? "Used" : "Relevant";
        var selectedPlacement = _cmbSuggestionPlacement.SelectedIndex == 1 ? "Above" : "Below";

        _profile.SetSetting("MinimizeToTray", _chkMinimizeToTray.Checked);
        _profile.SetSetting("EnableSounds", _chkEnableSounds.Checked);
        _profile.SetSetting("EnableAutoUpdates", _chkCheckUpdates.Checked);
        _profile.SetSetting("AIProvider", _activeProvider);
        _profile.SetSetting("APIKey", _activeApiKey);
        _profile.SetSetting("AIKeyValidated", _aiValidated);
        _profile.SetSetting("AIModel", SelectedModel());
        _profile.SetSetting("LocalMode", _chkLocalMode.Checked);
        _profile.SetSetting("BlockedApplications", blockedApps);
        _profile.SetSetting("Theme", selectedTheme);
        _profile.SetSetting("SuggestionSortMode", selectedSortMode);
        _profile.SetSetting("SuggestionPlacement", selectedPlacement);
        _profile.SetSetting("RequireConfirmationForEdits", _chkRequireConfirmation.Checked);
        _profile.SetSetting("GrammarSensitivity", _cmbGrammarSensitivity.SelectedItem?.ToString() ?? "Medium");
        _profile.SetSetting("MuteGrammarForCasualApps", _chkMuteCasualGrammar.Checked);
        _profile.SetSetting("EnableRewriteHotkey", _chkEnableRewriteHotkey.Checked);
        _profile.SetSetting("GrammarChecking", _chkGrammarChecking.Checked);
        _profile.SetSetting("EnableGrammarHotkey", _chkEnableGrammarHotkey.Checked);
        var mutedGrammar = _txtGrammarMutedApps.Text
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
        _profile.SetSetting("GrammarMutedApps", mutedGrammar);
        var toneRows = _lstAppTone.Items.Cast<object>().Select(i => i.ToString()!).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
        _profile.SetSetting("AppCategoryOverrides", toneRows);
        if (_editConfirmation != null)
        {
            _editConfirmation.RequireConfirmation = _chkRequireConfirmation.Checked;
        }

        _privacyGuard.ReplaceBlockedApplications(blockedApps);
        _suggestionPipeline?.SetSortMode(selectedSortMode);
        _suggestionOverlay?.SetPlacement(selectedPlacement);
        SetupMinimizeToTrayBehavior();
    }

    private void ShowAdvancedProviders()
    {
        _aiAdvancedVisible = true;
        _cmbAIProvider.Visible = true;
        _lnkMoreProviders.Visible = false;
        _lblAiRecommended.Text = "Provider";
        UpdateAiEntryMode();
    }

    private string SelectedUiProvider()
    {
        if (_aiAdvancedVisible)
        {
            return _cmbAIProvider.SelectedItem?.ToString() ?? AiProviderCatalog.Recommended;
        }

        return AiProviderCatalog.Recommended;
    }

    private void UpdateAiEntryMode()
    {
        var provider = SelectedUiProvider();
        var needsKey = AiProviderCatalog.UsesApiKey(provider) && !_chkLocalMode.Checked;
        _txtAPIKey.Visible = needsKey;
        _btnGetApiKey.Visible = needsKey;
        _lblAiModel.Visible = !provider.Equals("None", StringComparison.OrdinalIgnoreCase) && !_chkLocalMode.Checked;
        _cmbAiModel.Visible = _lblAiModel.Visible;
        if (_cmbAiModel.Visible)
        {
            FillModelChoices(provider, SelectedModel());
        }
        if (!needsKey)
        {
            StopClipboardWatch();
        }
        _lblAiRecommended.Visible = true;
        if (!_aiAdvancedVisible)
        {
            _lblAiRecommended.Text = "OpenAI (recommended) — paste an API key to connect.";
        }
        else if (provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase))
        {
            _lblAiRecommended.Text = "Ollama runs locally. No API key is required.";
        }
        else if (provider.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            _lblAiRecommended.Text = "No AI provider. Dictionary suggestions still work.";
        }
        else
        {
            _lblAiRecommended.Text = $"{provider} — paste an API key to connect.";
        }
    }

    private void ScheduleAiProbe()
    {
        if (_loading)
        {
            return;
        }

        _aiProbeTimer.Stop();
        _aiProbeTimer.Start();
    }

    private void SetAiStatus(string text, Color color)
    {
        _lblAiStatus.Text = text;
        _lblAiStatus.ForeColor = color;
    }

    private async Task ProbeAiAsync()
    {
        if (IsDisposed)
        {
            return;
        }

        if (_chkLocalMode.Checked)
        {
            SetAiStatus("Local-only mode is on, so AI is not used.", Color.FromArgb(140, 100, 0));
            return;
        }

        var provider = SelectedUiProvider();
        var key = _txtAPIKey.Text.Trim();

        if (provider.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            _activeProvider = "None";
            _activeApiKey = string.Empty;
            _aiValidated = true;
            SetAiStatus("AI provider disconnected.", Color.FromArgb(90, 90, 90));
            ApplyNow();
            _applyAiProvider?.Invoke(null);
            return;
        }

        if (AiProviderCatalog.UsesApiKey(provider) && string.IsNullOrEmpty(key))
        {
            if (!_watchingClipboard)
            {
                SetAiStatus("Paste a key to connect. LexFlow will check it automatically.", Color.FromArgb(90, 90, 90));
            }

            return;
        }

        var checking = provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase)
            ? "Checking for Ollama running locally…"
            : "Checking key…";
        SetAiStatus(checking, Color.FromArgb(0, 90, 160));

        _aiProbeCts?.Cancel();
        _aiProbeCts?.Dispose();
        _aiProbeCts = new CancellationTokenSource();
        var token = _aiProbeCts.Token;

        AiProbeResult result;
        try
        {
            result = await AiProviderCatalog.ProbeAsync(provider, key, token, SelectedModel());
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (IsDisposed || token.IsCancellationRequested)
        {
            return;
        }

        if (!result.Succeeded)
        {
            SetAiStatus(result.Message, Color.FromArgb(180, 40, 40));
            return;
        }

        _activeProvider = provider;
        _activeApiKey = AiProviderCatalog.UsesApiKey(provider) ? key : string.Empty;
        _aiValidated = true;
        EnsureDefaultModel(provider);
        SetAiStatus(provider.Equals("Ollama", StringComparison.OrdinalIgnoreCase) ? "Ollama found." : "Connected.", Color.FromArgb(0, 120, 80));
        ApplyNow();
        try
        {
            _applyAiProvider?.Invoke(AiProviderCatalog.Create(provider, key, model: SelectedModel()));
        }
        catch (Exception)
        {
            SetAiStatus("Connected, but LexFlow could not switch providers until restart.", Color.FromArgb(140, 100, 0));
        }
    }

    private const int WmClipboardUpdate = 0x031D;

    [DllImport("user32.dll")]
    private static extern bool AddClipboardFormatListener(IntPtr hwnd);

    [DllImport("user32.dll")]
    private static extern bool RemoveClipboardFormatListener(IntPtr hwnd);

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WmClipboardUpdate && _watchingClipboard)
        {
            TryFillKeyFromClipboard();
        }

        base.WndProc(ref m);
    }

    private void OnGetApiKeyClicked()
    {
        var provider = SelectedUiProvider();
        var url = AiProviderCatalog.KeyCreationUrl(provider);
        if (string.IsNullOrEmpty(url))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch
        {
            SetAiStatus("Could not open the key page. Paste a key here instead.", Color.FromArgb(180, 40, 40));
            return;
        }

        StartClipboardWatch(provider);
    }

    private void StartClipboardWatch(string provider)
    {
        StopClipboardWatch();
        if (!IsHandleCreated)
        {
            CreateHandle();
        }

        _waitingProvider = provider;
        if (!AddClipboardFormatListener(Handle))
        {
            SetAiStatus("Could not watch the clipboard. Paste your key here after you copy it.", Color.FromArgb(140, 100, 0));
            return;
        }

        _watchingClipboard = true;
        _btnCancelWait.Visible = true;
        _clipboardWatchTimeout.Stop();
        _clipboardWatchTimeout.Start();
        SetAiStatus("Waiting for you to copy your key from the browser…", Color.FromArgb(0, 90, 160));
    }

    private void StopClipboardWatch(string? status = null)
    {
        _clipboardWatchTimeout.Stop();
        if (_watchingClipboard && IsHandleCreated)
        {
            RemoveClipboardFormatListener(Handle);
        }

        _watchingClipboard = false;
        _waitingProvider = null;
        _btnCancelWait.Visible = false;
        if (!string.IsNullOrEmpty(status))
        {
            SetAiStatus(status, Color.FromArgb(90, 90, 90));
        }
    }

    private void TryFillKeyFromClipboard()
    {
        var provider = _waitingProvider;
        if (string.IsNullOrEmpty(provider) || !Clipboard.ContainsText())
        {
            return;
        }

        string text;
        try
        {
            text = Clipboard.GetText();
        }
        catch
        {
            return;
        }

        if (!AiProviderCatalog.LooksLikeApiKey(provider, text))
        {
            return;
        }

        var key = AiProviderCatalog.FirstLine(text);
        StopClipboardWatch();
        _txtAPIKey.Text = key;
        _aiProbeTimer.Stop();
        _ = ProbeAiAsync();
    }

    private void FillModelChoices(string provider, string? selected)
    {
        var models = AiProviderCatalog.Models(provider).ToList();
        if (models.Count == 0)
        {
            return;
        }

        var pick = string.IsNullOrWhiteSpace(selected) ? AiProviderCatalog.DefaultModel(provider) : selected;
        if (!models.Contains(pick))
        {
            models.Insert(0, pick);
        }

        var previous = _loading;
        _loading = true;
        _cmbAiModel.Items.Clear();
        foreach (var model in models)
        {
            _cmbAiModel.Items.Add(model);
        }

        var index = _cmbAiModel.Items.IndexOf(pick);
        _cmbAiModel.SelectedIndex = index >= 0 ? index : 0;
        _loading = previous;
    }

    private string SelectedModel()
    {
        var provider = SelectedUiProvider();
        return _cmbAiModel.SelectedItem?.ToString()
            ?? AiProviderCatalog.DefaultModel(provider);
    }

    private void EnsureDefaultModel(string provider)
    {
        if (_cmbAiModel.SelectedItem == null || _cmbAiModel.Items.Count == 0)
        {
            FillModelChoices(provider, AiProviderCatalog.DefaultModel(provider));
        }
    }

    private void OnAddBlockedAppClicked(object? sender, EventArgs e)
    {
        using var picker = new ProcessPickerForm();
        if (picker.ShowDialog() == DialogResult.OK && !string.IsNullOrEmpty(picker.SelectedProcessName))
        {
            var currentApps = _txtBlockedApps.Text
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();

            if (!currentApps.Contains(picker.SelectedProcessName))
            {
                currentApps.Add(picker.SelectedProcessName);
                _txtBlockedApps.Text = string.Join(", ", currentApps);
                ApplyNow();
            }
        }
    }

    private void OnAboutClicked(object? sender, EventArgs e)
    {
        using var aboutForm = new AboutForm();
        aboutForm.ShowDialog(this);
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

    private void OnThemeChanged(object? sender, EventArgs e)
    {
        if (_loading || _cmbTheme.SelectedItem == null || _themeManager == null)
        {
            return;
        }

        var selectedTheme = _cmbTheme.SelectedItem.ToString() ?? "Light";
        _cmbTheme.DroppedDown = false;
        _themeManager.SetTheme(selectedTheme);
        _cmbTheme.SelectedIndexChanged -= OnThemeChanged;
        try
        {
            ApplyTheme();
        }
        finally
        {
            _cmbTheme.SelectedIndexChanged += OnThemeChanged;
        }

        ApplyNow();
    }

    private void ApplyTheme()
    {
        if (_themeManager == null)
        {
            return;
        }

        ThemeUi.ApplyToTreeWithoutFlicker(this, _themeManager.CurrentTheme);
    }

    private void AddAppToneOverride()
    {
        var app = AppCategoryMapper.EnsureExeExtension(_txtAppTone.Text);
        if (string.IsNullOrEmpty(app) || !Enum.TryParse<AppWritingCategory>(_cmbAppToneCategory.SelectedItem?.ToString(), true, out var category))
        {
            return;
        }

        _txtAppTone.Text = app;
        if (!TryLocateApp(app, out var location))
        {
            MessageBox.Show(
                this,
                $"No application named {app} was found.\n\nIt is not running, and it was not found on the PATH or in Windows App Paths.\n\nThe tone override was not saved. Start the app and try again, or check the name.",
                "App tone",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return;
        }

        var row = AppCategoryMapper.FormatRow(app, category);
        for (var i = _lstAppTone.Items.Count - 1; i >= 0; i--)
        {
            if (AppCategoryMapper.TryParseRow(_lstAppTone.Items[i]?.ToString(), out var existing, out _) && existing == app)
            {
                _lstAppTone.Items.RemoveAt(i);
            }
        }

        _lstAppTone.Items.Add(row);
        ApplyNow();
        MessageBox.Show(
            this,
            $"{app} {location}.\n\n{category} tone has been saved for this app.",
            "App tone",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static bool TryLocateApp(string exeName, out string location)
    {
        location = string.Empty;
        var stem = Path.GetFileNameWithoutExtension(exeName);
        try
        {
            var running = Process.GetProcessesByName(stem);
            var isRunning = running.Length > 0;
            foreach (var process in running)
            {
                process.Dispose();
            }

            if (isRunning)
            {
                location = "is running";
                return true;
            }
        }
        catch
        {
            // Lookup must not throw.
        }

        if (IsOnPath(exeName) || IsRegisteredAppPath(exeName))
        {
            location = "was found on this PC";
            return true;
        }

        return false;
    }

    private static bool IsOnPath(string exeName)
    {
        try
        {
            if (File.Exists(Path.Combine(Environment.SystemDirectory, exeName)))
            {
                return true;
            }

            var path = Environment.GetEnvironmentVariable("PATH");
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            foreach (var directory in path.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                var trimmed = directory.Trim('"');
                if (File.Exists(Path.Combine(trimmed, exeName)))
                {
                    return true;
                }
            }
        }
        catch
        {
            return false;
        }

        return false;
    }

    private static bool IsRegisteredAppPath(string exeName)
    {
        var relative = @"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\" + exeName;
        try
        {
            using var hkcu = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(relative);
            if (hkcu != null)
            {
                return true;
            }

            using var hklm = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(relative);
            if (hklm != null)
            {
                return true;
            }

            using var wow = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\App Paths\" + exeName);
            return wow != null;
        }
        catch
        {
            return false;
        }
    }

    private void RemoveAppToneOverride()
    {
        if (_lstAppTone.SelectedIndex >= 0)
        {
            _lstAppTone.Items.RemoveAt(_lstAppTone.SelectedIndex);
            ApplyNow();
        }
    }

}
