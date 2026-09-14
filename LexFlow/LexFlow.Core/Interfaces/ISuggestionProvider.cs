namespace LexFlow.Core.Interfaces;

/// <summary>
/// Interface for providing text suggestions based on context
/// </summary>
public interface ISuggestionProvider
{
    string Name { get; }

    /// <summary>
    /// When true, this provider is queried on every keystroke.
    /// Slow providers (network AI) should return false so typing stays responsive.
    /// </summary>
    bool IsFastPath => true;

    Task<IEnumerable<Models.Suggestion>> GetSuggestionsAsync(Models.TextContext context, CancellationToken cancellationToken = default);
}
