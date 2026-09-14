using LexFlow.Core.Interfaces;
using LexFlow.Core.Models;
using LexFlow.Core.Learning;

namespace LexFlow.Core.Pipeline;

/// <summary>
/// Multi-stage suggestion pipeline that orchestrates multiple suggestion providers
/// </summary>
public class SuggestionPipeline : ISuggestionPipeline
{
    private readonly List<ISuggestionProvider> _providers = new();
    private readonly IPrivacyGuard _privacyGuard;
    private PersonalizationManager? _personalizationManager;
    private bool _isEnabled = true;
    private string _sortMode = "Relevant";

    public SuggestionPipeline(IPrivacyGuard privacyGuard)
    {
        _privacyGuard = privacyGuard ?? throw new ArgumentNullException(nameof(privacyGuard));
    }

    public bool IsEnabled => _isEnabled;

    public void SetEnabled(bool isEnabled)
    {
        _isEnabled = isEnabled;
    }

    public void SetSortMode(string sortMode)
    {
        _sortMode = string.Equals(sortMode, "Used", StringComparison.OrdinalIgnoreCase)
            ? "Used"
            : "Relevant";
    }

    /// <summary>
    /// Set the personalization manager for learning user preferences
    /// </summary>
    public void SetPersonalizationManager(PersonalizationManager personalizationManager)
    {
        _personalizationManager = personalizationManager;
    }

    public void AddProvider(ISuggestionProvider provider)
    {
        if (provider == null) throw new ArgumentNullException(nameof(provider));
        _providers.Add(provider);
    }

    public void RemoveProvider(string providerName)
    {
        _providers.RemoveAll(p => p.Name == providerName);
    }

    public async Task<IEnumerable<Suggestion>> GetSuggestionsAsync(TextContext context, CancellationToken cancellationToken = default)
    {
        return await CollectAsync(context, fastPathOnly: true, cancellationToken);
    }

    public async Task<IEnumerable<Suggestion>> GetSupplementalSuggestionsAsync(TextContext context, CancellationToken cancellationToken = default)
    {
        return await CollectAsync(context, fastPathOnly: false, cancellationToken);
    }

    private async Task<IEnumerable<Suggestion>> CollectAsync(TextContext context, bool fastPathOnly, CancellationToken cancellationToken)
    {
        if (!_isEnabled)
        {
            return Enumerable.Empty<Suggestion>();
        }

        if (_privacyGuard.IsSecureField(context))
        {
            return Enumerable.Empty<Suggestion>();
        }

        var fast = _providers.Where(p => p.IsFastPath).ToList();
        var slow = _providers.Where(p => !p.IsFastPath).ToList();
        List<ISuggestionProvider> providers;
        if (fastPathOnly)
        {
            providers = fast.Count > 0 ? fast : _providers.ToList();
        }
        else
        {
            providers = slow;
        }

        if (providers.Count == 0)
        {
            return Enumerable.Empty<Suggestion>();
        }

        var results = await Task.WhenAll(providers.Select(p => p.GetSuggestionsAsync(context, cancellationToken)));
        var allSuggestions = results.SelectMany(s => s).ToList();

        if (_personalizationManager != null && _personalizationManager.IsEnabled)
        {
            allSuggestions = allSuggestions.Select(s =>
            {
                var personalizedScore = _personalizationManager.GetPersonalizedScore(s, context);
                var keepScore = IsPinned(s);
                return new Suggestion
                {
                    Text = s.Text,
                    Source = s.Source,
                    Score = keepScore ? Math.Max(s.Score, 0.99) : personalizedScore,
                    Category = s.Category,
                    Metadata = s.Metadata
                };
            }).ToList();

            if (fastPathOnly)
            {
                var preferredSuggestions = _personalizationManager.GetPreferredSuggestions(context, 3);
                foreach (var preferred in preferredSuggestions)
                {
                    if (!allSuggestions.Any(s => s.Text.Equals(preferred, StringComparison.OrdinalIgnoreCase)))
                    {
                        allSuggestions.Add(new Suggestion
                        {
                            Text = preferred,
                            Source = "Personalized",
                            Score = 0.9
                        });
                    }
                }
            }
        }

        return RankAndDeduplicate(allSuggestions);
    }

    public IReadOnlyList<Suggestion> Rerank(IEnumerable<Suggestion> suggestions, TextContext context)
    {
        var list = suggestions?.ToList() ?? [];
        if (list.Count == 0 || _personalizationManager == null || !_personalizationManager.IsEnabled)
        {
            return RankAndDeduplicate(list).ToList();
        }

        var scored = list.Select(s => new Suggestion
        {
            Text = s.Text,
            Source = s.Source,
            Score = IsPinned(s) ? Math.Max(s.Score, 0.99) : _personalizationManager.GetPersonalizedScore(s, context),
            Category = s.Category,
            Metadata = s.Metadata
        });
        return RankAndDeduplicate(scored).ToList();
    }

    /// <summary>
    /// Record suggestion interaction for learning
    /// </summary>
    public void RecordInteraction(Suggestion suggestion, TextContext context, InteractionType interactionType)
    {
        _personalizationManager?.RecordInteraction(suggestion, context, interactionType);
    }

    /// <summary>
    /// Learn from user's writing style
    /// </summary>
    public void LearnWritingStyle(string userText, TextContext context, IReadOnlyList<string>? offeredCompletions = null)
    {
        if (string.IsNullOrWhiteSpace(userText))
        {
            return;
        }

        var words = UnfinishedWordFilter.Apply(
            CompletedWordExtractor.Extract(userText),
            offeredCompletions);

        if (words.Count == 0)
        {
            return;
        }

        _personalizationManager?.LearnWritingStyle(userText, context);

        foreach (var provider in _providers)
        {
            if (provider is ILearnableSuggestionProvider learnableProvider)
            {
                learnableProvider.LearnWords(words, offeredCompletions);
            }
        }
    }

    public IReadOnlyList<string> GetLearnedWords()
        => FirstLearnable()?.GetLearnedWords() ?? Array.Empty<string>();

    public void AddExplicitLearnedWord(string word)
        => FirstLearnable()?.AddExplicitWord(word);

    public void RemoveLearnedWord(string word)
        => FirstLearnable()?.RemoveLearnedWord(word);

    public void NeverLearnWord(string word)
        => FirstLearnable()?.NeverLearnWord(word);

    public void ClearLearnedWords()
        => FirstLearnable()?.ClearLearnedWords();

    public bool UndoLastLearn(TimeSpan? maxAge = null)
        => FirstLearnable()?.UndoLastLearn(maxAge) ?? false;

    private ILearnableSuggestionProvider? FirstLearnable()
        => _providers.OfType<ILearnableSuggestionProvider>().FirstOrDefault();

    private IEnumerable<Suggestion> RankAndDeduplicate(IEnumerable<Suggestion> suggestions)
    {
        var ranked = suggestions
            .GroupBy(s => s.Text)
            .Select(g => g.OrderByDescending(s => s.Score).First());

        IEnumerable<Suggestion> ordered = ranked
            .OrderByDescending(IsPinned)
            .ThenByDescending(s => s.Score);

        if (string.Equals(_sortMode, "Used", StringComparison.OrdinalIgnoreCase) && _personalizationManager != null)
        {
            ordered = ranked
                .OrderByDescending(IsPinned)
                .ThenByDescending(s => _personalizationManager.GetAcceptanceCount(s.Text))
                .ThenByDescending(s => s.Score);
        }

        return ordered.Take(40);
    }

    private static bool IsPinned(Suggestion suggestion)
        => string.Equals(suggestion.Source, "Grammar", StringComparison.Ordinal)
           || string.Equals(suggestion.Source, "Spelling", StringComparison.Ordinal);
}
