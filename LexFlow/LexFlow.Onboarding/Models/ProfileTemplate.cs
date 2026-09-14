namespace LexFlow.Onboarding.Models;

/// <summary>
/// Pre-configured profile templates for different use cases
/// </summary>
public class ProfileTemplate
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Dictionary<string, bool> FeatureDefaults { get; set; } = new();
    public Dictionary<string, object> PreferenceDefaults { get; set; } = new();
}

public static class ProfileTemplates
{
    public static ProfileTemplate Developer => new()
    {
        Id = "developer",
        Name = "Developer",
        Description = "Optimized for coding with code-aware suggestions and IDE integration",
        FeatureDefaults = new()
        {
            { "TextPredictionEnabled", true },
            { "ClipboardHistoryEnabled", true },
            { "TextImprovementEnabled", true },
            { "AIEnabled", false }, // Developers may prefer local-only
            { "AutoBrackets", true },
            { "AutoIndent", true },
            { "GrammarChecking", true }
        },
        PreferenceDefaults = new()
        {
            { "SuggestionAggressiveness", "Medium" },
            { "PerformanceMode", "Balanced" },
            { "Theme", "Dark" }
        }
    };

    public static ProfileTemplate Writer => new()
    {
        Id = "writer",
        Name = "Writer",
        Description = "Optimized for writing with grammar checking and style suggestions",
        FeatureDefaults = new()
        {
            { "TextPredictionEnabled", true },
            { "ClipboardHistoryEnabled", false },
            { "TextImprovementEnabled", true },
            { "AIEnabled", true },
            { "GrammarChecking", true },
            { "StyleSuggestions", true }
        },
        PreferenceDefaults = new()
        {
            { "SuggestionAggressiveness", "High" },
            { "PerformanceMode", "Quality" },
            { "Theme", "Light" }
        }
    };

    public static ProfileTemplate General => new()
    {
        Id = "general",
        Name = "General",
        Description = "Balanced configuration for everyday use across applications",
        FeatureDefaults = new()
        {
            { "TextPredictionEnabled", true },
            { "ClipboardHistoryEnabled", true },
            { "TextImprovementEnabled", false },
            { "AIEnabled", false },
            { "GrammarChecking", true }
        },
        PreferenceDefaults = new()
        {
            { "SuggestionAggressiveness", "Medium" },
            { "PerformanceMode", "Balanced" },
            { "Theme", "System" }
        }
    };

    public static IEnumerable<ProfileTemplate> All => new[] { Developer, Writer, General };
}
