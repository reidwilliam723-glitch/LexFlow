namespace LexFlow.Core.Models;

public class WritingStatsSnapshot
{
    public int SuggestionsAccepted { get; set; }
    public int SuggestionsRejected { get; set; }
    public int SuggestionsIgnored { get; set; }
    public int ExpansionUses { get; set; }
    public int CharactersInsertedByLexFlow { get; set; }
    public IReadOnlyList<(string Trigger, int Uses)> TopExpansions { get; set; } = [];
    public string? AdaptedToneNote { get; set; }

    public int KeystrokesSaved => Math.Max(0, CharactersInsertedByLexFlow);
    public double EstimatedSecondsSaved => KeystrokesSaved * 0.2;
}
