using LexFlow.Core.Models;

namespace LexFlow.Core;

public static class AiPromptContext
{
    public static string BuildSuggestionPrompt(TextContext context)
    {
        var prompt = "Complete the word or next few words at the caret. Return 3 short completions, one per line, no numbering.\n\n";
        var extras = BuildExtras(context);
        if (!string.IsNullOrEmpty(extras))
        {
            prompt += extras + "\n";
        }

        if (!string.IsNullOrEmpty(context.PreviousWords))
        {
            prompt += $"Text before caret: {context.PreviousWords}\n";
        }

        if (!string.IsNullOrEmpty(context.CurrentWord))
        {
            prompt += $"Partial word: {context.CurrentWord}\n";
        }

        if (!string.IsNullOrEmpty(context.FollowingWords))
        {
            prompt += $"Text after caret: {context.FollowingWords}\n";
        }

        prompt += "Completions:";
        return prompt;
    }

    public static string BuildRewritePrompt(string text, string instruction, TextContext? context = null)
    {
        var extras = context == null ? string.Empty : BuildExtras(context);
        var header = string.IsNullOrEmpty(extras) ? string.Empty : extras + "\n";
        return $"{header}Rewrite. Instruction: {instruction}\n\n{text}\n\nReturn only the rewritten text.";
    }

    public static string BuildIssuePrompt(string text, TextContext? context = null)
    {
        var extras = context == null ? string.Empty : BuildExtras(context);
        var header = string.IsNullOrEmpty(extras) ? string.Empty : extras + "\n";
        return $"{header}List grammar, clarity, and passive-voice issues in the text. " +
               "Return zero or more lines in the form ISSUE: short description. " +
               "If there are no issues, return NONE.\n\nText:\n" + text;
    }

    public static string BuildExtras(TextContext context)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(context.AppToneHint))
        {
            parts.Add($"Audience/tone: {context.AppToneHint}");
        }

        if (!string.IsNullOrWhiteSpace(context.WritingStyleHint))
        {
            parts.Add($"Match this writing style: {context.WritingStyleHint}");
        }

        return parts.Count == 0 ? string.Empty : string.Join("\n", parts);
    }
}
