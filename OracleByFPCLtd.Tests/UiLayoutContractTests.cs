using System;
using System.IO;
using System.Runtime.CompilerServices;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class UiLayoutContractTests
{
    private static readonly string RepoRoot = FindRepoRoot();

    [Fact]
    public void MainWindowUsesLockedTopLevelSpacingAndPadding()
    {
        var xamlPath = Path.Combine(RepoRoot, "OracleByFPCLtd", "MainWindow.xaml");
        Assert.True(File.Exists(xamlPath), $"MainWindow XAML not found: {xamlPath}");

        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("<Grid Margin=\"12,6,12,12\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Menu Grid.Row=\"0\" Grid.Column=\"0\" Margin=\"0,0,0,6\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Grid x:Name=\"TopHeaderGrid\" Grid.Row=\"1\" Grid.Column=\"0\" Margin=\"0,0,0,6\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ColumnDefinition Width=\"6\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ConnectBoxBorder\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ProjectDataBorder\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"StatusBorder\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Padding=\"6\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Height=\"{Binding ActualHeight, ElementName=ProjectDataBorder}\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"6\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ConnectionPanelUsesLockedDiscoverDropdownWidth()
    {
        var xamlPath = Path.Combine(RepoRoot, "OracleByFPCLtd", "UI", "Panels", "ConnectionPanel.xaml");
        Assert.True(File.Exists(xamlPath), $"ConnectionPanel XAML not found: {xamlPath}");

        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("<TextBox x:Name=\"IpTextBoxControl\" Width=\"180\" Height=\"26\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ComboBox x:Name=\"DiscoveredComboControl\" Width=\"180\" Height=\"26\" />", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectDataPanelUsesLockedRecentProjectDropdownWidth()
    {
        var xamlPath = Path.Combine(RepoRoot, "OracleByFPCLtd", "UI", "Panels", "ProjectDataPanel.xaml");
        Assert.True(File.Exists(xamlPath), $"ProjectDataPanel XAML not found: {xamlPath}");

        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("x:Name=\"UploadProjectButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"180\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ComboBox x:Name=\"RecentProjectComboBoxControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"180\"", xaml, StringComparison.Ordinal);
    }

    private static string FindRepoRoot([CallerFilePath] string sourceFilePath = "")
    {
        var testsDirectory = Path.GetDirectoryName(sourceFilePath);
        if (string.IsNullOrWhiteSpace(testsDirectory))
        {
            throw new DirectoryNotFoundException("Could not locate test source directory.");
        }

        return Path.GetFullPath(Path.Combine(testsDirectory, ".."));
    }
}
