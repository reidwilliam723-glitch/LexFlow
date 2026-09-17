using Lexon.Core.Learning;
using Xunit;

namespace Lexon.Tests;

public class UnfinishedWordFilterTests
{
    [Fact]
    public void Apply_DropsPrefixOfOfferedCompletion()
    {
        var kept = UnfinishedWordFilter.Apply(new[] { "hel", "lexon" }, new[] { "hello", "help" });

        Assert.Equal(new[] { "lexon" }, kept);
    }

    [Fact]
    public void Apply_KeepsWordWhenNoLongerOffer()
    {
        var kept = UnfinishedWordFilter.Apply(new[] { "lexon" }, new[] { "lexon" });

        Assert.Equal(new[] { "lexon" }, kept);
    }
}
