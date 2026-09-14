namespace LexFlow.Core.Models;

public sealed class AdaptationRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Summary { get; set; } = string.Empty;
    public string OutcomeKey { get; set; } = string.Empty;
    public DateTime OccurredUtc { get; set; } = DateTime.UtcNow;
    public bool Undone { get; set; }

    public string ToDisplay()
    {
        var stamp = OccurredUtc.ToLocalTime().ToString("d MMM");
        return Undone ? $"{stamp}: {Summary} (undone)" : $"{stamp}: {Summary}";
    }

    public override string ToString() => ToDisplay();
}
