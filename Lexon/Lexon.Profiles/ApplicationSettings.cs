using Lexon.Core.Interfaces;

namespace Lexon.Profiles;

/// <summary>
/// Manages application-specific settings
/// </summary>
public class ApplicationSettings
{
    private readonly IStorage _storage;
    private readonly Dictionary<string, ApplicationProfile> _applicationProfiles = new();

    public ApplicationSettings(IStorage storage)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
    }

    /// <summary>
    /// Loads persisted application settings. Must be awaited by the caller
    /// before relying on any profile data — the constructor can't be async,
    /// so this has to run separately rather than fire-and-forget.
    /// </summary>
    public async Task InitializeAsync()
    {
        var loaded = await _storage.LoadAsync<Dictionary<string, ApplicationProfile>>("application_settings");
        if (loaded != null)
        {
            foreach (var (key, value) in loaded)
            {
                _applicationProfiles[key] = value;
            }
        }
    }

    public ApplicationProfile GetProfile(string applicationName)
    {
        var normalizedName = NormalizeAppName(applicationName);
        
        if (!_applicationProfiles.TryGetValue(normalizedName, out var profile))
        {
            profile = new ApplicationProfile
            {
                ApplicationName = normalizedName,
                IsEnabled = true,
                Settings = new Dictionary<string, object>()
            };
            _applicationProfiles[normalizedName] = profile;
        }
        
        return profile;
    }

    public void SetProfile(string applicationName, ApplicationProfile profile)
    {
        var normalizedName = NormalizeAppName(applicationName);
        _applicationProfiles[normalizedName] = profile;
        SaveSettings();
    }

    public T GetSetting<T>(string applicationName, string key, T defaultValue = default!)
    {
        var profile = GetProfile(applicationName);
        
        if (profile.Settings.TryGetValue(key, out var value))
        {
            try
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }
        
        return defaultValue;
    }

    public void SetSetting<T>(string applicationName, string key, T value)
    {
        var profile = GetProfile(applicationName);
        profile.Settings[key] = value!;
        SaveSettings();
    }

    public void SetEnabled(string applicationName, bool enabled)
    {
        var profile = GetProfile(applicationName);
        profile.IsEnabled = enabled;
        SaveSettings();
    }

    public bool IsEnabled(string applicationName)
    {
        return GetProfile(applicationName).IsEnabled;
    }

    public IEnumerable<string> GetBlockedApplications()
    {
        return _applicationProfiles
            .Where(p => !p.Value.IsEnabled)
            .Select(p => p.Value.ApplicationName);
    }

    public void BlockApplication(string applicationName)
    {
        SetEnabled(applicationName, false);
    }

    public void UnblockApplication(string applicationName)
    {
        SetEnabled(applicationName, true);
    }

    private string NormalizeAppName(string applicationName)
    {
        return applicationName.ToLowerInvariant().Trim();
    }

    private void SaveSettings()
    {
        // Fire-and-forget is intentional here (settings writes shouldn't block
        // the caller), but failures are no longer silent/unhandled.
        _ = SaveSettingsAsync();
    }

    private async Task SaveSettingsAsync()
    {
        try
        {
            await _storage.SaveAsync("application_settings", _applicationProfiles);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save application settings: {ex.Message}");
        }
    }
}

public class ApplicationProfile
{
    public string ApplicationName { get; set; } = string.Empty;
    public bool IsEnabled { get; set; } = true;
    public Dictionary<string, object> Settings { get; set; } = new();
    public DateTime LastUsed { get; set; } = DateTime.UtcNow;
    public int UsageCount { get; set; }
}
