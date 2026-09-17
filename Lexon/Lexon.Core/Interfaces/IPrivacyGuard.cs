using Lexon.Core.Models;

namespace Lexon.Core.Interfaces;

/// <summary>
/// Interface for privacy guard that blocks suggestions in secure fields
/// </summary>
public interface IPrivacyGuard
{
    bool IsSecureField(TextContext context);
    bool IsApplicationBlocked(string applicationName);

    /// <summary>
    /// True when Lexon must not read, rewrite, or check this field or app.
    /// </summary>
    bool ShouldBlockAssistance(TextContext context);
}
