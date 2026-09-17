namespace Lexon.Core.Models;

/// <summary>
/// Next-word chips from the bigram model. Not a <see cref="Suggestion"/> —
/// several options are shown at once and they are not ranked through the
/// suggestion pipeline.
/// </summary>
public sealed class PredictedFollowers
{
    public string PreviousWord { get; init; } = string.Empty;
    public IReadOnlyList<string> Words { get; init; } = Array.Empty<string>();
}
