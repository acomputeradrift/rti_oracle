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
    public void ConnectionPanelUsesAlignedProcessorAndDiscoveryLayout()
    {
        var xamlPath = Path.Combine(RepoRoot, "OracleByFPCLtd", "UI", "Panels", "ConnectionPanel.xaml");
        Assert.True(File.Exists(xamlPath), $"ConnectionPanel XAML not found: {xamlPath}");

        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("Text=\"Processor IP:\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Discovered IPs:\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"IpTextBoxControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiscoveredComboControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ConnectButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Connect\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DisconnectButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Disconnect\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiscoverButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Discover\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"180\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"90\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ColumnDefinition Width=\"Auto\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("<ColumnDefinition Width=\"12\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalAlignment=\"Right\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Margin=\"0,6,0,0\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void ProjectDataPanelUsesAlignedSiblingSpacing()
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
        Assert.Contains("MaxWidth=\"160\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ColumnDefinition Width=\"12\" />", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Margin=\"10,0,0,0\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Margin=\"10,6,0,0\"", xaml, StringComparison.Ordinal);
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
        Assert.Contains("Margin=\"0,0,12,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiagnosticsZoomControlsBorder\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiagnosticsZoomOutButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiagnosticsZoomResetButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DiagnosticsZoomInButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Height=\"20\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Padding=\"6,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FilterApplyButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FilterClearButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FilterCountTextControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"ClearDiagnosticsButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"90\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Filter\" Width=\"90\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Clear\" Width=\"90\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Clear Logs\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Keywords:\"", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("Text=\"Filter: Keyword\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<TextBox x:Name=\"FilterKeywordTextBoxControl\" Grid.Column=\"1\" Width=\"180\" Height=\"24\" Margin=\"0,0,12,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Grid Grid.Column=\"3\" Width=\"180\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<TextBox x:Name=\"FilterStartTextBoxControl\" Grid.Column=\"0\" Width=\"156\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Start:\" VerticalAlignment=\"Center\" Margin=\"0,0,6,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Button x:Name=\"FilterStartPickerButtonControl\" Grid.Column=\"1\" Content=\"...\" Width=\"24\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Grid Grid.Column=\"3\" Width=\"180\" Margin=\"0,0,12,0\">", xaml, StringComparison.Ordinal);
        Assert.Contains("<Grid Grid.Column=\"5\" Width=\"180\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<TextBox x:Name=\"FilterEndTextBoxControl\" Grid.Column=\"0\" Width=\"156\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"End:\" VerticalAlignment=\"Center\" Margin=\"0,0,6,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Button x:Name=\"FilterEndPickerButtonControl\" Grid.Column=\"1\" Content=\"...\" Width=\"24\" Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Grid Grid.Column=\"5\" Width=\"180\" Margin=\"0,0,12,0\">", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Filter\" Width=\"90\" Height=\"24\" Padding=\"8,2\" Margin=\"0,0,6,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Content=\"Clear\" Width=\"90\" Height=\"24\" Padding=\"8,2\" Margin=\"0,0,6,0\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void FindBarUsesLockedSpacingAndFixedMatchWidth()
    {
        var xamlPath = Path.Combine(RepoRoot, "OracleByFPCLtd", "UI", "Controls", "FindBar.xaml");
        Assert.True(File.Exists(xamlPath), $"FindBar XAML not found: {xamlPath}");

        var xaml = File.ReadAllText(xamlPath);

        Assert.Contains("<Grid>", xaml, StringComparison.Ordinal);
        Assert.DoesNotContain("<ColumnDefinition Width=\"*\" />", xaml, StringComparison.Ordinal);
        Assert.Contains("Text=\"Find:\" VerticalAlignment=\"Center\" Margin=\"0,0,6,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FindTextBoxControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"140\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Height=\"24\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Margin=\"0,0,12,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FindPrevButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Margin=\"0,0,6,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FindNextButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FindClearButtonControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FindCountHostControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"FindCountTextControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Margin=\"0,0,0,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Width=\"76\"", xaml, StringComparison.Ordinal);
        Assert.Contains("TextAlignment=\"Left\"", xaml, StringComparison.Ordinal);
    }

    [Fact]
    public void OutputPanelsStretchFindBarToHeaderEdge()
    {
        var rawPath = Path.Combine(RepoRoot, "OracleByFPCLtd", "UI", "Panels", "RawOutputPanel.xaml");
        var processedPath = Path.Combine(RepoRoot, "OracleByFPCLtd", "UI", "Panels", "ProcessedOutputPanel.xaml");
        Assert.True(File.Exists(rawPath), $"RawOutputPanel XAML not found: {rawPath}");
        Assert.True(File.Exists(processedPath), $"ProcessedOutputPanel XAML not found: {processedPath}");

        var rawXaml = File.ReadAllText(rawPath);
        var processedXaml = File.ReadAllText(processedPath);

        Assert.Contains("<ColumnDefinition Width=\"Auto\" />", rawXaml, StringComparison.Ordinal);
        Assert.Contains("<ColumnDefinition Width=\"*\" />", rawXaml, StringComparison.Ordinal);
        Assert.Contains("<controls:FindBar x:Name=\"FindBarControl\" Grid.Column=\"1\" HorizontalAlignment=\"Right\" />", rawXaml, StringComparison.Ordinal);

        Assert.Contains("<ColumnDefinition Width=\"Auto\" />", processedXaml, StringComparison.Ordinal);
        Assert.Contains("<ColumnDefinition Width=\"*\" />", processedXaml, StringComparison.Ordinal);
        Assert.Contains("<controls:FindBar x:Name=\"FindBarControl\" Grid.Column=\"1\" HorizontalAlignment=\"Right\" />", processedXaml, StringComparison.Ordinal);
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
        Assert.Contains("Margin=\"0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<ItemsControl ItemsSource=\"{Binding Drivers}\" Margin=\"0,0,-12,-6\">", xaml, StringComparison.Ordinal);
        Assert.Contains("HorizontalContentAlignment=\"Stretch\"", xaml, StringComparison.Ordinal);
        Assert.Contains("x:Name=\"DriverCountTextBlockControl\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Margin=\"0,0,6,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("StackPanel Orientation=\"Horizontal\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Margin=\"6,0,0,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Margin=\"0,0,12,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("Margin=\"6,0,0,0\"", xaml, StringComparison.Ordinal);
        Assert.Contains("<Grid Height=\"24\" Margin=\"0,0,12,6\">", xaml, StringComparison.Ordinal);
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
