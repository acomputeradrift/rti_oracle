using System;
using System.IO;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class IconConfigurationTests
{
    [Fact]
    public void AppProjectDefinesApplicationIcon()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var appCsproj = Path.Combine(repoRoot, "OracleByFPCLtd", "OracleByFPCLtd.csproj");
        Assert.True(File.Exists(appCsproj), $"Project file not found: {appCsproj}");

        var csprojText = File.ReadAllText(appCsproj);
        Assert.Contains("<ApplicationIcon>Resources\\AppIcon.ico</ApplicationIcon>", csprojText, StringComparison.Ordinal);
    }

    [Fact]
    public void AppIconFileExists()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var iconPath = Path.Combine(repoRoot, "OracleByFPCLtd", "Resources", "AppIcon.ico");
        Assert.True(File.Exists(iconPath), $"Icon file not found: {iconPath}");
    }

    [Fact]
    public void MainAndAboutWindowsUseAppIcon()
    {
        var repoRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
        var mainWindowCodeBehind = Path.Combine(repoRoot, "OracleByFPCLtd", "MainWindow.xaml.cs");
        var aboutWindowCodeBehind = Path.Combine(repoRoot, "OracleByFPCLtd", "AboutWindow.xaml.cs");
        var iconLoader = Path.Combine(repoRoot, "OracleByFPCLtd", "WindowIconLoader.cs");
        Assert.True(File.Exists(mainWindowCodeBehind), $"MainWindow code-behind not found: {mainWindowCodeBehind}");
        Assert.True(File.Exists(aboutWindowCodeBehind), $"AboutWindow code-behind not found: {aboutWindowCodeBehind}");
        Assert.True(File.Exists(iconLoader), $"Window icon loader not found: {iconLoader}");

        Assert.Contains("WindowIconLoader.TryApply(this);", File.ReadAllText(mainWindowCodeBehind), StringComparison.Ordinal);
        Assert.Contains("WindowIconLoader.TryApply(this);", File.ReadAllText(aboutWindowCodeBehind), StringComparison.Ordinal);
    }
}
