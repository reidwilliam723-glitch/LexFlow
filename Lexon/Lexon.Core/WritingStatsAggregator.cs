using Lexon.Core.Expansion;
using Lexon.Core.Learning;
using Lexon.Core.Models;

namespace Lexon.Core;

public static class WritingStatsAggregator
{
    public static WritingStatsSnapshot Build(
        PersonalizationManager personalization,
        TextExpansionManager expansions,
        string? adaptedToneNote = null)
    {
        var top = expansions.GetAllExpansions()
            .Where(e => e.UsageCount > 0)
            .OrderByDescending(e => e.UsageCount)
            .Take(5)
            .Select(e => (e.Trigger, e.UsageCount))
            .ToList();

        return new WritingStatsSnapshot
        {
            SuggestionsAccepted = personalization.GetAcceptedTotal(),
            SuggestionsRejected = personalization.GetRejectedTotal(),
            SuggestionsIgnored = personalization.GetIgnoredTotal(),
            ExpansionUses = expansions.GetAllExpansions().Sum(e => e.UsageCount),
            CharactersInsertedByLexon = personalization.GetCharactersInserted(),
            TopExpansions = top,
            AdaptedToneNote = adaptedToneNote
        };
    }
}
