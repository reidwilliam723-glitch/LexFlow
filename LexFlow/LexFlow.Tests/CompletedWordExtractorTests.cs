using LexFlow.Core.Learning;
using Xunit;

namespace LexFlow.Tests;

public class CompletedWordExtractorTests
{
    [Fact]
    public void Extract_DropsInProgressLastWord()
    {
        var words = CompletedWordExtractor.Extract("hello hel");

        Assert.Equal(new[] { "hello" }, words);
    }

    [Fact]
    public void Extract_KeepsWordsWhenTextEndsWithSpace()
    {
        var words = CompletedWordExtractor.Extract("hello world ");

        Assert.Equal(new[] { "hello", "world" }, words);
    }

    [Fact]
    public void Extract_SingleIncompleteWord_ReturnsEmpty()
    {
        var words = CompletedWordExtractor.Extract("hel");

        Assert.Empty(words);
    }

    [Fact]
    public void Extract_CompletedWithPeriod_KeepsWord()
    {
        var words = CompletedWordExtractor.Extract("LexFlow.");

        Assert.Equal(new[] { "LexFlow" }, words);
    }
}
