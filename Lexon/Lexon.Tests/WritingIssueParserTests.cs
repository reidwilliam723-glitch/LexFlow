using Lexon.Core;
using Xunit;

namespace Lexon.Tests;

public class WritingIssueParserTests
{
    [Fact]
    public void ParsesIssueLines()
    {
        var issues = WritingIssueParser.Parse("ISSUE: passive voice\nISSUE: missing comma\nNONE");
        Assert.Equal(2, issues.Count);
        Assert.Equal("passive voice", issues[0]);
    }

    [Fact]
    public void None_IsEmpty()
    {
        Assert.Empty(WritingIssueParser.Parse("NONE"));
    }
}
