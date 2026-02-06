using System.Linq;
using VariableSubscribeProbe;
using Xunit;

namespace VariableSubscribeProbe.Tests;

public sealed class SelectionParserTests
{
    [Fact]
    public void ParseHandlesRangesAndSingles()
    {
        var result = SelectionParser.Parse("1,3-5,8");

        Assert.Equal(new[] { 1, 3, 4, 5, 8 }, result.ToArray());
    }
}
