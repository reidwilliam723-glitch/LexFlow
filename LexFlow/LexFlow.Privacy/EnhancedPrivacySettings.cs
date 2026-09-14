using System.Windows.Forms;

namespace LexFlow.Privacy;

/// <summary>
/// Enhanced privacy settings with explanations and user-friendly descriptions
/// </summary>
public class EnhancedPrivacySettings : UserControl
{
    private Dictionary<string, PrivacySetting> _settings = new();
    
    public event EventHandler<PrivacySettingChangedEventArgs>? SettingChanged;

    public EnhancedPrivacySettings()
    {
        InitializeComponent();
        InitializeDefaultSettings();
    }

    private void InitializeComponent()
    {
        this.Size = new Size(600, 500);
        this.BackColor = Color.White;

        var titleLabel = new Label
        {
            Text = "Privacy Settings",
            Font = new Font("Segoe UI", 14, FontStyle.Bold),
            Location = new Point(20, 20),
            AutoSize = true
        };

        var descriptionLabel = new Label
        {
            Text = "Configure how LexFlow handles your data and privacy. Learn more about each setting below.",
            Font = new Font("Segoe UI", 10),
            Location = new Point(20, 50),
            Size = new Size(560, 40),
            ForeColor = Color.FromArgb(80, 80, 80)
        };

        this.Controls.Add(titleLabel);
        this.Controls.Add(descriptionLabel);

        var y = 100;
        foreach (var setting in GetPrivacySettings())
        {
            var settingControl = CreatePrivacySettingControl(setting, y);
            this.Controls.Add(settingControl);
            y += 120;
        }
    }

    private Control CreatePrivacySettingControl(PrivacySetting setting, int y)
    {
        var panel = new Panel
        {
            Location = new Point(20, y),
            Size = new Size(560, 110),
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = Color.FromArgb(250, 250, 250)
        };

        var checkbox = new CheckBox
        {
            Text = setting.Name,
            Location = new Point(10, 10),
            Size = new Size(300, 24),
            Font = new Font("Segoe UI", 10, FontStyle.Bold),
            Checked = setting.Enabled
        };
        checkbox.CheckedChanged += (s, e) => OnSettingChanged(setting.Id, checkbox.Checked);

        var descriptionLabel = new Label
        {
            Text = setting.Description,
            Location = new Point(30, 35),
            Size = new Size(520, 40),
            Font = new Font("Segoe UI", 9),
            ForeColor = Color.FromArgb(100, 100, 100)
        };

        var explanationLabel = new Label
        {
            Text = $"ℹ️ {setting.Explanation}",
            Location = new Point(30, 75),
            Size = new Size(520, 30),
            Font = new Font("Segoe UI", 8, FontStyle.Italic),
            ForeColor = Color.FromArgb(80, 80, 80)
        };

        panel.Controls.Add(checkbox);
        panel.Controls.Add(descriptionLabel);
        panel.Controls.Add(explanationLabel);

        return panel;
    }

    private void InitializeDefaultSettings()
    {
        foreach (var setting in GetPrivacySettings())
        {
            _settings[setting.Id] = setting;
        }
    }

    private List<PrivacySetting> GetPrivacySettings()
    {
        return new List<PrivacySetting>
        {
            new PrivacySetting
            {
                Id = "local_only_mode",
                Name = "Local-Only Mode",
                Description = "Process all data locally without sending anything to cloud services",
                Explanation = "When enabled, LexFlow will only use local suggestion sources (dictionary, learned patterns). AI features will be disabled. This provides maximum privacy but may reduce suggestion quality.",
                Enabled = false,
                Category = PrivacyCategory.DataProcessing
            },
            new PrivacySetting
            {
                Id = "secure_field_detection",
                Name = "Secure Field Detection",
                Description = "Automatically detect and block suggestions in password fields and credential dialogs",
                Explanation = "LexFlow uses window class names and UI patterns to identify secure fields. Suggestions are automatically blocked in these contexts to prevent accidental data exposure.",
                Enabled = true,
                Category = PrivacyCategory.SecureFields
            },
            new PrivacySetting
            {
                Id = "blocked_applications",
                Name = "Application Blocking",
                Description = "Block suggestions in specific applications (e.g., password managers, banking apps)",
                Explanation = "You can specify applications where LexFlow should never show suggestions. This is useful for apps that handle sensitive information.",
                Enabled = true,
                Category = PrivacyCategory.ApplicationControl
            },
            new PrivacySetting
            {
                Id = "data_retention",
                Name = "Data Retention",
                Description = "Control how long learned data is stored on your device",
                Explanation = "LexFlow learns from your typing patterns to improve suggestions. You can choose how long to retain this data. Shorter retention periods improve privacy but may reduce suggestion accuracy.",
                Enabled = true,
                Category = PrivacyCategory.DataRetention
            },
            new PrivacySetting
            {
                Id = "encryption_at_rest",
                Name = "Encryption at Rest",
                Description = "Encrypt all stored data using AES-GCM encryption",
                Explanation = "All data stored by LexFlow is encrypted using military-grade AES-GCM encryption. The encryption key is protected by Windows DPAPI, which ties it to your user account.",
                Enabled = true,
                Category = PrivacyCategory.Encryption
            },
            new PrivacySetting
            {
                Id = "audit_logging",
                Name = "Audit Logging",
                Description = "Log all suggestion requests for privacy auditing",
                Explanation = "Keep a detailed log of when suggestions were requested, which applications made requests, and what data was processed. This helps you monitor LexFlow's activity.",
                Enabled = false,
                Category = PrivacyCategory.Auditing
            },
            new PrivacySetting
            {
                Id = "cloud_data_anonymization",
                Name = "Cloud Data Anonymization",
                Description = "Anonymize data before sending to cloud AI services",
                Explanation = "When using cloud AI features, LexFlow can strip personally identifiable information before sending data. This reduces privacy risks when using AI suggestions.",
                Enabled = true,
                Category = PrivacyCategory.CloudServices
            },
            new PrivacySetting
            {
                Id = "transmission_monitoring",
                Name = "Transmission Monitoring",
                Description = "Monitor and display all data transmissions to cloud services",
                Explanation = "Show real-time information about data being sent to cloud services, including size, destination, and frequency. This helps you understand when and how your data is being transmitted.",
                Enabled = true,
                Category = PrivacyCategory.CloudServices
            }
        };
    }

    private void OnSettingChanged(string settingId, bool enabled)
    {
        if (_settings.TryGetValue(settingId, out var setting))
        {
            setting.Enabled = enabled;
            SettingChanged?.Invoke(this, new PrivacySettingChangedEventArgs
            {
                SettingId = settingId,
                Enabled = enabled
            });
        }
    }

    public bool GetSetting(string settingId)
    {
        return _settings.TryGetValue(settingId, out var setting) ? setting.Enabled : false;
    }

    public void SetSetting(string settingId, bool enabled)
    {
        if (_settings.TryGetValue(settingId, out var setting))
        {
            setting.Enabled = enabled;
        }
    }

    public IEnumerable<PrivacySetting> GetSettingsByCategory(PrivacyCategory category)
    {
        return _settings.Values.Where(s => s.Category == category);
    }
}

public class PrivacySetting
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public bool Enabled { get; set; }
    public PrivacyCategory Category { get; set; }
}

public enum PrivacyCategory
{
    DataProcessing,
    SecureFields,
    ApplicationControl,
    DataRetention,
    Encryption,
    Auditing,
    CloudServices
}

public class PrivacySettingChangedEventArgs : EventArgs
{
    public string SettingId { get; set; } = string.Empty;
    public bool Enabled { get; set; }
}
