using System.Media;
using System.IO;

namespace Lexon.Service;

/// <summary>
/// Manages sound feedback for user interactions
/// </summary>
public class SoundFeedbackManager
{
    private readonly Dictionary<string, string> _soundPaths = new();
    private readonly Dictionary<string, byte[]> _defaultSounds = new();
    private bool _enabled = true;
    private float _volume = 0.5f;

    public bool Enabled
    {
        get => _enabled;
        set => _enabled = value;
    }

    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0, 1);
    }

    public SoundFeedbackManager()
    {
        InitializeDefaultSounds();
        LoadCustomSounds();
    }

    private void InitializeDefaultSounds()
    {
        // Generate simple beep sounds for default feedback
        _defaultSounds["suggestion_show"] = GenerateBeep(800, 50);
        _defaultSounds["suggestion_accept"] = GenerateBeep(1000, 100);
        _defaultSounds["suggestion_dismiss"] = GenerateBeep(400, 50);
        _defaultSounds["error"] = GenerateBeep(200, 200);
        _defaultSounds["learning"] = GenerateBeep(600, 30);
    }

    private byte[] GenerateBeep(int frequency, int duration)
    {
        // This is a placeholder - in a real implementation, you'd generate actual WAV data
        // For now, we'll use SystemSounds
        return Array.Empty<byte>();
    }

    private void LoadCustomSounds()
    {
        var soundDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Lexon",
            "Sounds"
        );

        if (Directory.Exists(soundDirectory))
        {
            var soundFiles = Directory.GetFiles(soundDirectory, "*.wav");
            foreach (var file in soundFiles)
            {
                var soundName = Path.GetFileNameWithoutExtension(file);
                _soundPaths[soundName] = file;
            }
        }
    }

    public void PlaySound(string soundName)
    {
        if (!_enabled) return;

        try
        {
            if (_soundPaths.TryGetValue(soundName, out var customPath))
            {
                using var player = new SoundPlayer(customPath);
                player.Play();
            }
            else
            {
                // Use system sounds for default feedback
                PlaySystemSound(soundName);
            }
        }
        catch
        {
            // Silently fail if sound can't be played
        }
    }

    private void PlaySystemSound(string soundName)
    {
        var systemSound = soundName switch
        {
            "suggestion_show" => SystemSounds.Asterisk,
            "suggestion_accept" => SystemSounds.Exclamation,
            "suggestion_dismiss" => SystemSounds.Beep,
            "error" => SystemSounds.Hand,
            "learning" => SystemSounds.Question,
            _ => null
        };

        systemSound?.Play();
    }

    public void SetCustomSound(string soundName, string filePath)
    {
        if (File.Exists(filePath))
        {
            _soundPaths[soundName] = filePath;
            SaveSoundConfiguration();
        }
    }

    public void ResetToDefault(string soundName)
    {
        _soundPaths.Remove(soundName);
        SaveSoundConfiguration();
    }

    private void SaveSoundConfiguration()
    {
        var configDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "Lexon"
        );

        Directory.CreateDirectory(configDirectory);

        var configPath = Path.Combine(configDirectory, "sound_config.json");
        // Save configuration logic would go here
    }
}
