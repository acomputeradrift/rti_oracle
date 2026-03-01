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

        Assert.Contains("<TextBox x:Name=\"IpTextBoxControl\" Width=\"180\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Button x:Name=\"ConnectButtonControl\" Content=\"Connect\" Width=\"90\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Button x:Name=\"DisconnectButtonControl\" Content=\"Disconnect\" Width=\"90\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Button x:Name=\"DiscoverButtonControl\" Content=\"Discover\" Width=\"90\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ComboBox x:Name=\"DiscoveredComboControl\" Width=\"180\" Height=\"24\" />", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectDataPanelUsesLockedRecentProjectDropdownWidth()
    {
        var xamlPath = Path.Combine(RepoRoot, "OracleByFPCLtd", "UI", "Panels", "ProjectDataPanel.xaml");
        Assert.True(File.Exists(xamlPath), $"ProjectDataPanel XAML not found: {xamlPath}");

        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("x:Name=\"UploadProjectButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"180\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ComboBox x:Name=\"RecentProjectComboBoxControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"180\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Height=\"24\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void FilterBarUsesLockedStandardControlHeights()
    {
        var xamlPath = Path.Combine(RepoRoot, "OracleByFPCLtd", "UI", "Controls", "FilterBar.xaml");
        Assert.True(File.Exists(xamlPath), $"FilterBar XAML not found: {xamlPath}");

        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("x:Name=\"DiagnosticsHeaderTextControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FontSize=\"16\"", xaml, StringComparison.Ordinal);
        Assert.Contains("FontWeight=\"Bold\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiagnosticsZoomControlsBorder\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiagnosticsZoomOutButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiagnosticsZoomResetButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiagnosticsZoomInButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Height=\"20\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Padding=\"6,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<TextBox x:Name=\"FilterKeywordTextBoxControl\" Grid.Column=\"1\" MinWidth=\"200\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<TextBox x:Name=\"FilterStartTextBoxControl\" Grid.Column=\"0\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Button x:Name=\"FilterStartPickerButtonControl\" Grid.Column=\"1\" Content=\"...\" Width=\"24\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<TextBox x:Name=\"FilterEndTextBoxControl\" Grid.Column=\"0\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Button x:Name=\"FilterEndPickerButtonControl\" Grid.Column=\"1\" Content=\"...\" Width=\"24\" Height=\"24\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void DriverLogLevelsPanelUsesLockedLayoutContract()
    {
        var xamlPath = Path.Combine(RepoRoot, "OracleByFPCLtd", "UI", "Panels", "DriverLogLevelsPanel.xaml");
        Assert.True(File.Exists(xamlPath), $"DriverLogLevelsPanel XAML not found: {xamlPath}");

        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("<Setter Property=\"Height\" Value=\"30\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Height\" Value=\"20\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<Setter Property=\"Height\" Value=\"24\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<UniformGrid Columns=\"4\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DriverLogLevelsExpandedHost\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Margin=\"0,6,0,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ItemsControl ItemsSource=\"{Binding Drivers}\" Margin=\"0,0,-6,-6\">", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalContentAlignment=\"Stretch\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DriverCountTextBlockControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Margin=\"0,0,6,0\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<ScrollViewer", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<Grid Grid.Row=\"0\" Margin=\"0,0,0,6\">", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void MainWindowUpdatesDriverCountFromVisibleDrivers()
    {
        var codePath = Path.Combine(RepoRoot, "OracleByFPCLtd", "MainWindow.xaml.cs");
        Assert.True(File.Exists(codePath), $"MainWindow code-behind not found: {codePath}");

        var code = File.ReadAllText(codePath);

        Assert.Contains("DriverLogLevelsPanel.SetDriverCount(GetVisibleLogLevelDriversSnapshot().Count);", code, StringComparison.Ordinal);
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
