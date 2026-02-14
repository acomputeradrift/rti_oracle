using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Navigation;

namespace OracleByFPCLtd;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();
        WindowIconLoader.TryApply(this);
        var label = AppVersion.CurrentLabel();
        VersionLink.Inlines.Clear();
        VersionLink.Inlines.Add(new Run(label));
        Title = $"About {AppVersion.CurrentLabel()}";
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
        {
            UseShellExecute = true
        });
        e.Handled = true;
    }

    private void VersionLink_Click(object sender, RoutedEventArgs e)
    {
        var changelog = LoadChangelogForDisplay();
        ShowChangelogWindow(changelog);
    }

    private static string LoadChangelogForDisplay()
    {
        var outputPath = Path.Combine(AppContext.BaseDirectory, "CHANGELOG.md");
        if (File.Exists(outputPath))
        {
            return NormalizeChangelogForDisplay(File.ReadAllText(outputPath));
        }

        var repoPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ApplicationPackaging", "CHANGELOG.md"));
        if (File.Exists(repoPath))
        {
            return NormalizeChangelogForDisplay(File.ReadAllText(repoPath));
        }

        return "Changelog not found.";
    }

    private static string NormalizeChangelogForDisplay(string rawChangelog)
    {
        var lines = rawChangelog.Replace("\r\n", "\n").Split('\n');
        var firstReleasedSectionIndex = lines
            .Select((line, index) => new { line, index })
            .FirstOrDefault(item => item.line.StartsWith("## [", StringComparison.Ordinal) && !item.line.StartsWith("## [Unreleased]", StringComparison.Ordinal))
            ?.index ?? -1;

        if (firstReleasedSectionIndex < 0)
        {
            return rawChangelog;
        }

        return string.Join(Environment.NewLine, lines.Skip(firstReleasedSectionIndex)).Trim();
    }

    private void ShowChangelogWindow(string changelog)
    {
        var window = new Window
        {
            Title = "Changelog",
            Width = 760,
            Height = 560,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            Content = new TextBox
            {
                Text = changelog,
                IsReadOnly = true,
                TextWrapping = TextWrapping.Wrap,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                FontFamily = new System.Windows.Media.FontFamily("Consolas")
            }
        };

        window.ShowDialog();
    }
}
