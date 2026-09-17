using Lexon.Onboarding.Models;
using Lexon.Storage;
using Lexon.Profiles;
using Lexon.Core.Interfaces;

namespace Lexon.Onboarding.Wizard;

/// <summary>
/// First-run wizard for step-by-step configuration
/// </summary>
public partial class OnboardingWizard : Form
{
    private readonly IStorage _storage;
    private readonly Profile _profile;
    private OnboardingState _state = new();
    private int _currentStep = 0;
    private List<WizardStep>? _steps;

    public OnboardingWizard(IStorage storage, Profile profile)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        
        InitializeSteps();
        InitializeComponent();
        LoadState();
    }

    private void InitializeComponent()
    {
        AutoScaleMode = AutoScaleMode.Dpi;
        this.Text = "Welcome to Lexon - Setup Wizard";
        this.Size = new Size(900, 650);
        this.StartPosition = FormStartPosition.CenterScreen;
        this.FormBorderStyle = FormBorderStyle.Sizable;
        this.MinimumSize = new Size(800, 600);
        this.BackColor = Color.White;
        this.Font = new Font("Segoe UI", 10);

        // Header
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = Color.FromArgb(45, 45, 48),
            Padding = new Padding(30, 20, 30, 20)
        };
        
        var titleLabel = new Label
        {
            Text = "Lexon Setup",
            Font = new Font("Segoe UI", 22, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(0, 10),
            AutoSize = true
        };
        
        var subtitleLabel = new Label
        {
            Text = "Let's get you started with personalized suggestions",
            Font = new Font("Segoe UI", 11),
            ForeColor = Color.FromArgb(200, 200, 200),
            Location = new Point(0, 45),
            AutoSize = true
        };
        
        headerPanel.Controls.Add(titleLabel);
        headerPanel.Controls.Add(subtitleLabel);
        this.Controls.Add(headerPanel);

        // Content panel
        var contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(40, 30, 40, 30)
        };
        
        _stepPanel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true
        };
        
        contentPanel.Controls.Add(_stepPanel);
        this.Controls.Add(contentPanel);

        // Footer with navigation
        var footerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 80,
            BackColor = Color.FromArgb(245, 245, 245),
            Padding = new Padding(30, 20, 30, 20)
        };
        
        _progressBar = new ProgressBar
        {
            Location = new Point(30, 25),
            Size = new Size(450, 15),
            Style = ProgressBarStyle.Continuous
        };
        
        _backButton = CreateButton("← Back", 520, 20);
        _backButton.Click += OnBackClicked;
        _backButton.Enabled = false;
        
        _nextButton = CreateButton("Next →", 630, 20);
        _nextButton.Click += OnNextClicked;
        
        footerPanel.Controls.Add(_progressBar);
        footerPanel.Controls.Add(_backButton);
        footerPanel.Controls.Add(_nextButton);
        this.Controls.Add(footerPanel);

        LoadStep(0);
    }

    private Panel _stepPanel = null!;
    private ProgressBar _progressBar = null!;
    private Button _backButton = null!;
    private Button _nextButton = null!;

    private void InitializeSteps()
    {
        _steps = new List<WizardStep>
        {
            new WelcomeStep(),
            new PrivacyDisclosureStep(),
            new ProfileSelectionStep(),
            new FeatureToggleStep(),
            new PreferencesStep(),
            new CompletionStep()
        };
    }

    private void LoadState()
    {
        var savedState = _storage.LoadAsync<OnboardingState>("onboarding_state").Result;
        if (savedState != null && savedState.HasCompletedOnboarding)
        {
            _state = savedState;
            _currentStep = _steps.Count - 1; // Skip to completion
        }
        else
        {
            _state = new OnboardingState
            {
                OnboardingStarted = DateTime.UtcNow,
                CurrentStep = 0
            };
        }
    }

    private void LoadStep(int stepIndex)
    {
        if (stepIndex < 0 || stepIndex >= _steps.Count) return;

        _currentStep = stepIndex;
        _stepPanel.Controls.Clear();
        
        var step = _steps[stepIndex];
        step.Initialize(_stepPanel, _state);
        
        _progressBar.Value = (int)((stepIndex + 1) * 100.0 / _steps.Count);
        _backButton.Enabled = stepIndex > 0;
        _nextButton.Text = stepIndex == _steps.Count - 1 ? "Finish" : "Next →";
    }

    private void OnBackClicked(object? sender, EventArgs e)
    {
        LoadStep(_currentStep - 1);
    }

    private async void OnNextClicked(object? sender, EventArgs e)
    {
        var currentStep = _steps[_currentStep];
        if (!currentStep.Validate())
        {
            MessageBox.Show("Please complete all required fields.", "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        currentStep.Save(_state);

        if (_currentStep < _steps.Count - 1)
        {
            LoadStep(_currentStep + 1);
        }
        else
        {
            // This is the root of the async chain and is bound directly to a
            // WinForms Click event, so it has to be async void — but the
            // awaited work itself lives in an async Task method (below) and
            // any failure is caught here instead of crashing the process.
            try
            {
                await CompleteOnboardingAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Something went wrong finishing setup: {ex.Message}", "Setup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private async Task CompleteOnboardingAsync()
    {
        _state.HasCompletedOnboarding = true;
        _state.OnboardingCompleted = DateTime.UtcNow;
        
        await _storage.SaveAsync("onboarding_state", _state);
        
        // Apply selected profile settings
        var template = ProfileTemplates.All.FirstOrDefault(p => p.Id == _state.SelectedProfile);
        if (template != null)
        {
            foreach (var feature in template.FeatureDefaults)
            {
                _profile.SetSetting(feature.Key, feature.Value);
            }
            foreach (var pref in template.PreferenceDefaults)
            {
                _profile.SetSetting(pref.Key, pref.Value);
            }
        }
        
        // Apply user preferences
        foreach (var pref in _state.UserPreferences)
        {
            _profile.SetSetting(pref.Key, pref.Value);
        }
        
        // Apply feature toggles
        foreach (var toggle in _state.FeatureToggles)
        {
            var key = toggle.Key switch
            {
                "Automatic grammar" => "GrammarChecking",
                "Local Mode Only" => "LocalMode",
                _ => toggle.Key
            };
            _profile.SetSetting(key, toggle.Value);
        }
        
        await _profile.SaveAsync();
        
        MessageBox.Show("Setup complete! Lexon is now configured for your needs.", "Welcome to Lexon", MessageBoxButtons.OK, MessageBoxIcon.Information);
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private Button CreateButton(string text, int x, int y)
    {
        return new Button
        {
            Text = text,
            Location = new Point(x, y),
            Size = new Size(120, 40),
            Font = new Font("Segoe UI", 10),
            BackColor = Color.FromArgb(0, 120, 215),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
    }
}

public abstract class WizardStep
{
    public abstract void Initialize(Panel panel, OnboardingState state);
    public virtual bool Validate() => true;
    public virtual void Save(OnboardingState state) { }
}

public class WelcomeStep : WizardStep
{
    public override void Initialize(Panel panel, OnboardingState state)
    {
        var stack = WizardLayout.CreateStack();
        stack.Controls.Add(WizardLayout.Title("Welcome to Lexon!", size: 20f, bottomMargin: 30));
        stack.Controls.Add(WizardLayout.Body(
            "Lexon is your intelligent typing assistant that learns your writing style and provides smart suggestions across all your applications.\n\nThis wizard will help you configure Lexon to match your needs."));
        stack.Controls.Add(WizardLayout.Body(
            "Key Features:\n• Smart autocomplete that learns from your writing\n• Privacy-first design with local-only mode\n• AI-powered text improvement (optional)\n• Works across all Windows applications",
            bottomMargin: 0));
        panel.Controls.Add(stack);
    }
}

public class PrivacyDisclosureStep : WizardStep
{
    public override void Initialize(Panel panel, OnboardingState state)
    {
        var stack = WizardLayout.CreateStack();
        stack.Controls.Add(WizardLayout.Title("What Lexon can see — and what leaves your PC"));
        stack.Controls.Add(WizardLayout.Body(
            "Lexon watches the field you are typing in so it can offer local suggestions. That includes the words around the caret in the focused app.\n\n" +
            "What stays on this computer:\n" +
            "• Typing suggestions, spelling corrections, and grammar checks (rule-based, no cloud)\n" +
            "• Learned vocabulary and writing style\n\n" +
            "What can leave this computer:\n" +
            "• Only text you explicitly send to a configured cloud AI provider (for example, a selection rewrite via the Aa chip or Ctrl+Alt+R).\n" +
            "• If you never add an API key, or you turn on local-only mode, nothing is sent to OpenAI, Gemini, or DeepSeek.\n\n" +
            "Password fields, blocked apps, and other excluded windows are skipped — including rewrite and grammar, not just suggestions.\n\n" +
            "Double-press Ctrl at any time to fully disable Lexon until you turn it back on.",
            new Font("Segoe UI", 11),
            Color.FromArgb(50, 50, 50),
            bottomMargin: 0));
        panel.Controls.Add(stack);
    }
}

public class ProfileSelectionStep : WizardStep
{
    private ComboBox _profileComboBox = null!;

    public override void Initialize(Panel panel, OnboardingState state)
    {
        var stack = WizardLayout.CreateStack();
        stack.Controls.Add(WizardLayout.Title("Choose Your Profile", bottomMargin: 30));
        stack.Controls.Add(WizardLayout.Body(
            "Select a profile that best matches how you'll use Lexon. You can customize this later."));

        _profileComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 12),
            Width = 550
        };

        foreach (var profile in ProfileTemplates.All)
        {
            _profileComboBox.Items.Add(new { Name = profile.Name, Description = profile.Description, Id = profile.Id });
        }
        _profileComboBox.DisplayMember = "Name";
        _profileComboBox.ValueMember = "Id";

        if (!string.IsNullOrEmpty(state.SelectedProfile))
        {
            _profileComboBox.SelectedIndex = ProfileTemplates.All.ToList().FindIndex(p => p.Id == state.SelectedProfile);
        }
        else
        {
            _profileComboBox.SelectedIndex = 0;
        }

        var profileDescriptionLabel = WizardLayout.Body(
            string.Empty,
            new Font("Segoe UI", 11, FontStyle.Italic),
            Color.FromArgb(80, 80, 80),
            bottomMargin: 0);

        void UpdateProfileDescription()
        {
            var selectedItem = _profileComboBox.SelectedItem;
            if (selectedItem != null && selectedItem.GetType().GetProperty("Description") != null)
            {
                profileDescriptionLabel.Text = selectedItem.GetType().GetProperty("Description")?.GetValue(selectedItem)?.ToString() ?? string.Empty;
            }
        }

        _profileComboBox.SelectedIndexChanged += (_, _) => UpdateProfileDescription();
        UpdateProfileDescription();

        stack.Controls.Add(WizardLayout.FieldRow("Profile", _profileComboBox));
        stack.Controls.Add(profileDescriptionLabel);
        panel.Controls.Add(stack);
    }

    public override bool Validate()
    {
        return _profileComboBox.SelectedIndex >= 0;
    }

    public override void Save(OnboardingState state)
    {
        var selectedItem = _profileComboBox.SelectedItem;
        if (selectedItem != null && selectedItem.GetType().GetProperty("Id") != null)
        {
            var id = selectedItem.GetType().GetProperty("Id")?.GetValue(selectedItem)?.ToString();
            if (id != null)
            {
                state.SelectedProfile = id;
            }
        }
    }
}

public class FeatureToggleStep : WizardStep
{
    private Dictionary<string, CheckBox> _featureCheckboxes = new();

    public override void Initialize(Panel panel, OnboardingState state)
    {
        var stack = WizardLayout.CreateStack();
        stack.Controls.Add(WizardLayout.Title("Configure Features", bottomMargin: 30));
        stack.Controls.Add(WizardLayout.Body(
            "Enable or disable features. You can change these later in settings."));

        var features = new[]
        {
            ("Text Prediction", "Show suggestions as you type", true),
            ("Clipboard History", "Remember your clipboard contents", true),
            ("Automatic grammar", "Check spelling and grammar after you pause typing — no shortcut required", true),
            ("Text Improvement", "Optional cloud rewrite of selected text (requires an API key)", false),
            ("Local Mode Only", "Disable all cloud features for maximum privacy", false)
        };

        foreach (var (name, description, defaultValue) in features)
        {
            var checkbox = new CheckBox
            {
                Text = name,
                Checked = state.FeatureToggles.TryGetValue(name, out var value) ? value : defaultValue
            };

            _featureCheckboxes[name] = checkbox;
            stack.Controls.Add(WizardLayout.FeatureRow(checkbox, description));
        }

        panel.Controls.Add(stack);
    }

    public override void Save(OnboardingState state)
    {
        foreach (var (name, checkbox) in _featureCheckboxes)
        {
            state.FeatureToggles[name] = checkbox.Checked;
        }
    }
}

public class PreferencesStep : WizardStep
{
    private ComboBox _aggressivenessComboBox = null!;
    private ComboBox _performanceComboBox = null!;
    private ComboBox _themeComboBox = null!;

    public override void Initialize(Panel panel, OnboardingState state)
    {
        var stack = WizardLayout.CreateStack();
        stack.Controls.Add(WizardLayout.Title("Personalize Your Experience", bottomMargin: 30));
        stack.Controls.Add(WizardLayout.Body("Adjust these settings to match your preferences."));

        _aggressivenessComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 12),
            Width = 350
        };
        _aggressivenessComboBox.Items.AddRange(new[] { "Low", "Medium", "High" });
        _aggressivenessComboBox.SelectedIndex = 1;

        _performanceComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 12),
            Width = 350
        };
        _performanceComboBox.Items.AddRange(new[] { "Battery Saver", "Balanced", "Maximum Quality" });
        _performanceComboBox.SelectedIndex = 1;

        _themeComboBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Font = new Font("Segoe UI", 12),
            Width = 350
        };
        _themeComboBox.Items.AddRange(new[] { "Light", "Dark", "System" });
        _themeComboBox.SelectedIndex = 2;

        stack.Controls.Add(WizardLayout.FieldRow("Suggestion Aggressiveness:", _aggressivenessComboBox));
        stack.Controls.Add(WizardLayout.FieldRow("Performance Mode:", _performanceComboBox));
        stack.Controls.Add(WizardLayout.FieldRow("Theme:", _themeComboBox));
        panel.Controls.Add(stack);
    }

    public override void Save(OnboardingState state)
    {
        state.UserPreferences["SuggestionAggressiveness"] = _aggressivenessComboBox.SelectedItem?.ToString() ?? "Medium";
        state.UserPreferences["PerformanceMode"] = _performanceComboBox.SelectedItem?.ToString() ?? "Balanced";
        state.UserPreferences["Theme"] = _themeComboBox.SelectedItem?.ToString() ?? "System";
    }
}

public class CompletionStep : WizardStep
{
    public override void Initialize(Panel panel, OnboardingState state)
    {
        var stack = WizardLayout.CreateStack();
        stack.Controls.Add(WizardLayout.Title(
            "You're All Set!",
            size: 20f,
            foreColor: Color.FromArgb(16, 185, 129),
            bottomMargin: 30));
        stack.Controls.Add(WizardLayout.Body(
            $"Lexon has been configured with the {state.SelectedProfile} profile.\n\nYou can always adjust these settings later by right-clicking the Lexon icon in your system tray and selecting Settings."));
        stack.Controls.Add(WizardLayout.Body(
            "Quick Tips:\n• Press Tab to accept a suggestion\n• Press Esc to dismiss suggestions\n• Double-press Ctrl to quickly disable/enable Lexon\n• Right-click the system tray icon for quick access to settings",
            foreColor: Color.FromArgb(80, 80, 80),
            bottomMargin: 0));
        panel.Controls.Add(stack);
    }
}
