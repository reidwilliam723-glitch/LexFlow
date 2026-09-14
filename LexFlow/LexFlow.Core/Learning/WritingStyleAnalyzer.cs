using LexFlow.Core.Models;
using System.Text.RegularExpressions;

namespace LexFlow.Core.Learning;

public static class WritingStyleAnalyzer
{
    private static readonly Regex SentenceSplit = new(@"[.!?]+(?:\s+|$)", RegexOptions.Compiled);
    private static readonly Regex WordSplit = new(@"\b[\w']+\b", RegexOptions.Compiled);
    private static readonly HashSet<string> Contractions = new(StringComparer.OrdinalIgnoreCase)
    {
        "don't", "doesn't", "didn't", "can't", "couldn't", "won't", "wouldn't",
        "isn't", "aren't", "wasn't", "weren't", "i'm", "you're", "we're", "they're",
        "it's", "that's", "there's", "here's", "i've", "you've", "we've", "they've",
        "i'll", "you'll", "we'll", "they'll", "i'd", "you'd", "we'd", "they'd"
    };
    private static readonly HashSet<string> FirstPerson = new(StringComparer.OrdinalIgnoreCase)
    {
        "i", "i'm", "i've", "i'll", "i'd", "me", "my", "mine", "we", "we're", "we've", "us", "our"
    };

    public static WritingStyleProfile Analyze(string text)
    {
        var profile = new WritingStyleProfile();
        if (string.IsNullOrWhiteSpace(text))
        {
            return profile;
        }

        var sentences = SentenceSplit.Split(text).Select(s => s.Trim()).Where(s => s.Length > 0).ToList();
        if (sentences.Count == 0)
        {
            sentences.Add(text.Trim());
        }

        var words = WordSplit.Matches(text).Select(m => m.Value).ToList();
        if (words.Count == 0)
        {
            return profile;
        }

        profile.SampleCount = sentences.Count;
        profile.AverageSentenceLength = words.Count / (double)sentences.Count;
        profile.ContractionRate = words.Count(Contractions.Contains) / (double)words.Count;
        profile.FirstPersonRate = words.Count(FirstPerson.Contains) / (double)words.Count;
        profile.ExclamationRate = text.Count(c => c == '!') / (double)Math.Max(1, sentences.Count);
        return profile;
    }

    public static void Merge(WritingStyleProfile target, WritingStyleProfile sample)
    {
        if (sample.SampleCount <= 0)
        {
            return;
        }

        var total = target.SampleCount + sample.SampleCount;
        if (total <= 0)
        {
            return;
        }

        double Mix(double current, double incoming) =>
            ((current * target.SampleCount) + (incoming * sample.SampleCount)) / total;

        target.AverageSentenceLength = Mix(target.AverageSentenceLength, sample.AverageSentenceLength);
        target.ContractionRate = Mix(target.ContractionRate, sample.ContractionRate);
        target.FirstPersonRate = Mix(target.FirstPersonRate, sample.FirstPersonRate);
        target.ExclamationRate = Mix(target.ExclamationRate, sample.ExclamationRate);
        target.SampleCount = total;
    }
}
