using OracleByFPCLtd.Reliability;
using Xunit;

namespace OracleByFPCLtd.Tests.Reliability;

public sealed class UnhandledReportFilePrefixBuilderTests
{
    [Fact]
    public void BuildReturnsBasePrefixWhenNoProjectPath()
    {
        var result = UnhandledReportFilePrefixBuilder.Build(null);

        Assert.Equal("Oracle_Unhandled", result);
    }

    [Fact]
    public void BuildIncludesSanitizedProjectName()
    {
        var result = UnhandledReportFilePrefixBuilder.Build(@"C:\Projects\Dash OS v53.1 ? Demo.apex");

        Assert.Equal("Oracle_Unhandled_Dash_OS_v53.1__Demo", result);
    }
}
