using LexFlow.Core.Learning;
using Xunit;

namespace LexFlow.Tests;

public class UnfinishedWordFilterTests
{
    [Fact]
    public void Apply_DropsPrefixOfOfferedCompletion()
    {
        var kept = UnfinishedWordFilter.Apply(new[] { "hel", "lexflow" }, new[] { "hello", "help" });

        Assert.Equal(new[] { "lexflow" }, kept);
    }

    [Fact]
    public void Apply_KeepsWordWhenNoLongerOffer()
    {
        var kept = UnfinishedWordFilter.Apply(new[] { "lexflow" }, new[] { "lexflow" });

        Assert.Equal(new[] { "lexflow" }, kept);
    }
}
