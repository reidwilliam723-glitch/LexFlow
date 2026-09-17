namespace Lexon.Core.Models;

public class WritingStyleProfile
{
    public double AverageSentenceLength { get; set; }
    public double ContractionRate { get; set; }
    public double FirstPersonRate { get; set; }
    public double ExclamationRate { get; set; }
    public double SampleCount { get; set; }

    public string ToPromptHint()
    {
        if (SampleCount < 3)
        {
            return string.Empty;
        }

        var parts = new List<string>();
        if (AverageSentenceLength > 0 && AverageSentenceLength < 10)
        {
            parts.Add("short sentences");
        }
        else if (AverageSentenceLength > 22)
        {
            parts.Add("longer, more developed sentences");
        }

        if (ContractionRate >= 0.08)
        {
            parts.Add("frequent contractions");
        }
        else if (SampleCount >= 8 && ContractionRate < 0.02)
        {
            parts.Add("avoid contractions");
        }

        if (FirstPersonRate >= 0.08)
        {
            parts.Add("first-person voice");
        }

        if (ExclamationRate >= 0.04)
        {
            parts.Add("comfortable with emphasis");
        }

        return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
    }

    public string ToSummary()
    {
        if (SampleCount < 3)
        {
            return "Not enough writing sampled yet.";
        }

        var hint = ToPromptHint();
        if (string.IsNullOrEmpty(hint))
        {
            return "A balanced, neutral writing voice.";
        }

        return char.ToUpperInvariant(hint[0]) + hint[1..] + ".";
    }
}
