using LexFlow.Core.Learning;
using Xunit;

namespace LexFlow.Tests;

public class WritingStyleAnalyzerTests
{
    [Fact]
    public void ShortContractedText_HasShortSentencesAndContractions()
    {
        var profile = WritingStyleAnalyzer.Analyze("I don't know. It's fine. We'll see.");
        Assert.True(profile.AverageSentenceLength < 8);
        Assert.True(profile.ContractionRate > 0.2);
        Assert.True(profile.FirstPersonRate > 0.1);
        Assert.Contains("short sentences", profile.ToPromptHint());
        Assert.Contains("contractions", profile.ToPromptHint());
        Assert.Contains("Short sentences", profile.ToSummary());
    }
}
