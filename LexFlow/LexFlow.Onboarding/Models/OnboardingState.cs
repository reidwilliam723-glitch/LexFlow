namespace LexFlow.Onboarding.Models;

/// <summary>
/// Tracks the state of the onboarding process
/// </summary>
public class OnboardingState
{
    public bool HasCompletedOnboarding { get; set; }
    public int CurrentStep { get; set; }
    public DateTime? OnboardingStarted { get; set; }
    public DateTime? OnboardingCompleted { get; set; }
    public string SelectedProfile { get; set; } = string.Empty;
    public Dictionary<string, bool> FeatureToggles { get; set; } = new();
    public Dictionary<string, object> UserPreferences { get; set; } = new();
}
