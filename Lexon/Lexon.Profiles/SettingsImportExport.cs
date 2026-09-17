using Lexon.Core.Interfaces;
using System.Text.Json;

namespace Lexon.Profiles;

/// <summary>
/// Handles settings import/export functionality
/// </summary>
public class SettingsImportExport
{
    private readonly IStorage _storage;
    private readonly Profile _profile;

    public SettingsImportExport(IStorage storage, Profile profile)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
    }

    public async Task<string> ExportSettingsAsync(bool includePersonalData = false)
    {
        var exportData = new SettingsExport
        {
            Version = "1.0",
            ExportDate = DateTime.UtcNow,
            Settings = new Dictionary<string, object>(),
            IncludePersonalData = includePersonalData
        };

        // Export profile settings
        foreach (var setting in _profile.Settings)
        {
            // Filter out personal data if requested
            if (!includePersonalData && IsPersonalData(setting.Key))
            {
                continue;
            }

            exportData.Settings[setting.Key] = setting.Value!;
        }

        // Export metadata
        exportData.Metadata = new SettingsMetadata
        {
            ProfileName = _profile.Name,
            ProfileId = _profile.Id,
            CreatedAt = _profile.CreatedAt,
            LastModified = _profile.LastModified
        };

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        return json;
    }

    public async Task<bool> ImportSettingsAsync(string json, bool overwrite = false)
    {
        try
        {
            var importData = JsonSerializer.Deserialize<SettingsExport>(json);
            if (importData == null) return false;

            // Validate version compatibility
            if (!IsVersionCompatible(importData.Version))
            {
                return false;
            }

            // Import settings
            foreach (var setting in importData.Settings)
            {
                if (overwrite || !_profile.Settings.ContainsKey(setting.Key))
                {
                    _profile.SetSetting(setting.Key, setting.Value);
                }
            }

            await _profile.SaveAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task ExportToFileAsync(string filePath, bool includePersonalData = false)
    {
        var json = await ExportSettingsAsync(includePersonalData);
        await File.WriteAllTextAsync(filePath, json);
    }

    public async Task<bool> ImportFromFileAsync(string filePath, bool overwrite = false)
    {
        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            return await ImportSettingsAsync(json, overwrite);
        }
        catch
        {
            return false;
        }
    }

    private bool IsPersonalData(string key)
    {
        var personalDataKeys = new[]
        {
            "APIKey",
            "AuthToken",
            "PersonalInfo",
            "CustomDictionary",
            "LearnedWords"
        };

        return personalDataKeys.Any(key.Contains);
    }

    private bool IsVersionCompatible(string version)
    {
        // Simple version check - in production, use proper semantic versioning
        return version.StartsWith("1.");
    }
}

public class SettingsExport
{
    public string Version { get; set; } = string.Empty;
    public DateTime ExportDate { get; set; }
    public Dictionary<string, object> Settings { get; set; } = new();
    public SettingsMetadata? Metadata { get; set; }
    public bool IncludePersonalData { get; set; }
}

public class SettingsMetadata
{
    public string ProfileName { get; set; } = string.Empty;
    public string ProfileId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime LastModified { get; set; }
}
