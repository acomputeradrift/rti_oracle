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
        var mainWindowXaml = Path.Combine(repoRoot, "OracleByFPCLtd", "MainWindow.xaml");
        var aboutWindowXaml = Path.Combine(repoRoot, "OracleByFPCLtd", "AboutWindow.xaml");
        Assert.True(File.Exists(mainWindowXaml), $"MainWindow not found: {mainWindowXaml}");
        Assert.True(File.Exists(aboutWindowXaml), $"AboutWindow not found: {aboutWindowXaml}");

        var expectedIcon = "Icon=\"pack://application:,,,/Resources/AppIcon.ico\"";
        Assert.Contains(expectedIcon, File.ReadAllText(mainWindowXaml), StringComparison.Ordinal);
        Assert.Contains(expectedIcon, File.ReadAllText(aboutWindowXaml), StringComparison.Ordinal);
    }
}
