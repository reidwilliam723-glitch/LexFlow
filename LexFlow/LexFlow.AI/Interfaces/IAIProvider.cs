using LexFlow.AI;
using LexFlow.Core.Interfaces;
using LexFlow.Core.Models;

namespace LexFlow.AI.Interfaces;

/// <summary>
/// Interface for AI-powered suggestion providers
/// </summary>
public interface IAIProvider : ISuggestionProvider
{
    Task<string> RewriteTextAsync(
        string text,
        string instruction,
        CancellationToken cancellationToken = default,
        IProgress<string>? progress = null);
    Task<string> ImproveGrammarAsync(string text, CancellationToken cancellationToken = default);
    Task<string> ChangeToneAsync(string text, string tone, CancellationToken cancellationToken = default);
    Task<AiProbeResult> ProbeAsync(CancellationToken cancellationToken = default);
    Task WarmupAsync(CancellationToken cancellationToken = default);
}
