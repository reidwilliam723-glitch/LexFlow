using LexFlow.Core.Interfaces;
using LexFlow.Core.Models;

namespace LexFlow.Core.Grammar;

/// <summary>
/// Offers the current grammar fix in the suggestion list so it can be accepted with Tab.
/// </summary>
public sealed class GrammarSuggestionProvider : ISuggestionProvider
{
    public string Name => GrammarSuggestionMapper.Source;
    public bool IsFastPath => true;
    public Func<bool>? IsEnabled { get; set; }

    public Task<IEnumerable<Suggestion>> GetSuggestionsAsync(TextContext context, CancellationToken cancellationToken = default)
    {
        if (IsEnabled != null && !IsEnabled())
        {
            return Task.FromResult(Enumerable.Empty<Suggestion>());
        }

        IEnumerable<Suggestion> suggestions = GrammarSuggestionMapper.Suggest(context);
        return Task.FromResult(suggestions);
    }
}
