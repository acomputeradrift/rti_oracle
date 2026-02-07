using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class MainWindowFindTests
{
    [Fact]
    public void RawFindHighlightsAndCentersMatches()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow
            {
                Width = 900,
                Height = 700
            };
            window.Show();
            window.UpdateLayout();

            var rawLog = GetRawLogTextBox(window);
            var rawFind = GetRawFindTextBox(window);
            var countText = GetRawFindCountText(window);
            var prevButton = GetRawFindPrevButton(window);
            var nextButton = GetRawFindNextButton(window);

            var lines = BuildLines(40, 20, 30);
            InvokeSetRawOutput(window, lines);
            rawFind.Text = "match";
            InvokeRawFind(window);
            FlushLayout(window);

            Assert.Equal("Match: 1/2", countText.Text);
            Assert.True(prevButton.IsEnabled);
            Assert.True(nextButton.IsEnabled);
            Assert.Equal("match", GetSelectionText(rawLog));

            var firstMatchLine = GetFirstMatchLineIndex(lines);
            AssertLineIsVisible(rawLog, firstMatchLine);

            nextButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            FlushLayout(window);

            var secondMatchLine = GetSecondMatchLineIndex(lines);
            Assert.Equal("match", GetSelectionText(rawLog));
            AssertLineIsVisible(rawLog, secondMatchLine);
            Assert.Equal("Match: 2/2", countText.Text);

            prevButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            FlushLayout(window);

            Assert.Equal("match", GetSelectionText(rawLog));
            AssertLineIsVisible(rawLog, firstMatchLine);
            Assert.Equal("Match: 1/2", countText.Text);

            window.Hide();
        });
    }

    [Fact]
    public void ProcessedFindHighlightsAndCentersMatches()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow
            {
                Width = 900,
                Height = 700
            };
            window.Show();
            window.UpdateLayout();

            var processedLog = GetProcessedLogTextBox(window);
            var processedFind = GetProcessedFindTextBox(window);
            var countText = GetProcessedFindCountText(window);
            var prevButton = GetProcessedFindPrevButton(window);
            var nextButton = GetProcessedFindNextButton(window);

            var lines = BuildLines(40, 20, 30);
            InvokeSetProcessedOutput(window, lines);
            processedFind.Text = "match";
            InvokeProcessedFind(window);
            FlushLayout(window);

            Assert.Equal("Match: 1/2", countText.Text);
            Assert.True(prevButton.IsEnabled);
            Assert.True(nextButton.IsEnabled);
            Assert.Equal("match", GetSelectionText(processedLog));

            var firstMatchLine = GetFirstMatchLineIndex(lines);
            AssertProcessedLineIsCentered(processedLog, firstMatchLine);

            nextButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            FlushLayout(window);

            var secondMatchLine = GetSecondMatchLineIndex(lines);
            Assert.Equal("match", GetSelectionText(processedLog));
            AssertProcessedLineIsCentered(processedLog, secondMatchLine);
            Assert.Equal("Match: 2/2", countText.Text);

            prevButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            FlushLayout(window);

            Assert.Equal("match", GetSelectionText(processedLog));
            AssertProcessedLineIsCentered(processedLog, firstMatchLine);
            Assert.Equal("Match: 1/2", countText.Text);

            window.Hide();
        });
    }

    private static List<string> BuildLines(int total, int firstMatchIndex, int secondMatchIndex)
    {
        var lines = new List<string>(total);
        for (var i = 0; i < total; i++)
        {
            if (i == firstMatchIndex || i == secondMatchIndex)
            {
                lines.Add($"Line {i:00} match content");
            }
            else
            {
                lines.Add($"Line {i:00} content");
            }
        }

        return lines;
    }

    private static int GetFirstMatchLineIndex(IReadOnlyList<string> lines)
    {
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains("match", StringComparison.Ordinal))
            {
                return i;
            }
        }

        return -1;
    }

    private static int GetSecondMatchLineIndex(IReadOnlyList<string> lines)
    {
        var found = 0;
        for (var i = 0; i < lines.Count; i++)
        {
            if (lines[i].Contains("match", StringComparison.Ordinal))
            {
                found++;
                if (found == 2)
                {
                    return i;
                }
            }
        }

        return -1;
    }

    private static void AssertLineIsVisible(RichTextBox richTextBox, int expectedLineIndex)
    {
        var selectionLine = GetProcessedLineIndex(richTextBox, richTextBox.Selection.Start);
        Assert.Equal(expectedLineIndex, selectionLine);

        var scrollViewer = FindVisualChild<ScrollViewer>(richTextBox);
        Assert.NotNull(scrollViewer);

        var rect = richTextBox.Selection.Start.GetCharacterRect(LogicalDirection.Forward);
        var yInViewport = rect.Top - scrollViewer!.VerticalOffset + (rect.Height / 2);
        Assert.InRange(yInViewport, 0, scrollViewer.ViewportHeight);
    }

    private static void AssertProcessedLineIsCentered(RichTextBox richTextBox, int expectedLineIndex)
    {
        var selectionLine = GetProcessedLineIndex(richTextBox, richTextBox.Selection.Start);
        Assert.Equal(expectedLineIndex, selectionLine);

        var scrollViewer = FindVisualChild<ScrollViewer>(richTextBox);
        Assert.NotNull(scrollViewer);

        var rect = richTextBox.Selection.Start.GetCharacterRect(LogicalDirection.Forward);
        var yInViewport = rect.Top - scrollViewer!.VerticalOffset + (rect.Height / 2);
        Assert.InRange(yInViewport, 0, scrollViewer.ViewportHeight);
    }

    [Fact]
    public void FindShowsNoneWhenNoMatches()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow
            {
                Width = 900,
                Height = 700
            };
            window.Show();
            window.UpdateLayout();

            var rawLog = GetRawLogTextBox(window);
            var rawFind = GetRawFindTextBox(window);
            var countText = GetRawFindCountText(window);
            var prevButton = GetRawFindPrevButton(window);
            var nextButton = GetRawFindNextButton(window);

            InvokeSetRawOutput(window, new[] { "No entries here" });
            rawFind.Text = "absent";
            InvokeRawFind(window);
            FlushLayout(window);

            Assert.Equal("Match: None", countText.Text);
            Assert.False(prevButton.IsEnabled);
            Assert.False(nextButton.IsEnabled);

            window.Hide();
        });
    }

    [Fact]
    public void FindHighlightsAllMatchesInBothLogs()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow
            {
                Width = 900,
                Height = 700
            };
            window.Show();
            window.UpdateLayout();

            var rawLog = GetRawLogTextBox(window);
            var rawFind = GetRawFindTextBox(window);
            var processedLog = GetProcessedLogTextBox(window);
            var processedFind = GetProcessedFindTextBox(window);

            var lines = BuildLines(20, 4, 10);
            InvokeSetRawOutput(window, lines);
            InvokeSetProcessedOutput(window, lines);

            rawFind.Text = "match";
            processedFind.Text = "match";
            InvokeRawFind(window);
            InvokeProcessedFind(window);
            FlushLayout(window);

            Assert.True(CountHighlightedRuns(rawLog) >= 2);
            Assert.True(CountHighlightedRuns(processedLog) >= 2);

            window.Hide();
        });
    }

    [Fact]
    public void FindUsesFocusedHighlightForCurrentMatch()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow
            {
                Width = 900,
                Height = 700
            };
            window.Show();
            window.UpdateLayout();

            var rawLog = GetRawLogTextBox(window);
            var rawFind = GetRawFindTextBox(window);
            var rawNext = GetRawFindNextButton(window);
            var processedLog = GetProcessedLogTextBox(window);
            var processedFind = GetProcessedFindTextBox(window);
            var processedNext = GetProcessedFindNextButton(window);

            var lines = BuildLines(12, 2, 6);
            InvokeSetRawOutput(window, lines);
            InvokeSetProcessedOutput(window, lines);

            rawFind.Text = "match";
            processedFind.Text = "match";
            InvokeRawFind(window);
            InvokeProcessedFind(window);
            FlushLayout(window);

            Assert.Equal(1, CountRunsWithBackground(rawLog, FocusHighlightColor));
            Assert.True(CountRunsWithBackground(rawLog, MatchHighlightColor) >= 1);
            Assert.Equal(1, CountRunsWithBackground(processedLog, FocusHighlightColor));
            Assert.True(CountRunsWithBackground(processedLog, MatchHighlightColor) >= 1);

            rawNext.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            processedNext.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            FlushLayout(window);

            Assert.Equal(1, CountRunsWithBackground(rawLog, FocusHighlightColor));
            Assert.Equal(1, CountRunsWithBackground(processedLog, FocusHighlightColor));

            window.Hide();
        });
    }

    private static int GetProcessedLineIndex(RichTextBox richTextBox, TextPointer selectionStart)
    {
        var paragraph = richTextBox.Document.Blocks.FirstBlock as Paragraph;
        if (paragraph == null)
        {
            return 0;
        }

        var lineIndex = 0;
        foreach (var inline in paragraph.Inlines)
        {
            if (inline is Run run)
            {
                if (run.ContentStart.CompareTo(selectionStart) <= 0 && run.ContentEnd.CompareTo(selectionStart) >= 0)
                {
                    return lineIndex;
                }
            }
            else if (inline is LineBreak)
            {
                lineIndex++;
            }
        }

        return lineIndex;
    }

    private static string GetSelectionText(RichTextBox richTextBox)
    {
        var range = new TextRange(richTextBox.Selection.Start, richTextBox.Selection.End);
        return range.Text.Trim();
    }

    private static int CountHighlightedRuns(RichTextBox richTextBox)
    {
        var count = 0;
        foreach (var block in richTextBox.Document.Blocks)
        {
            if (block is not Paragraph paragraph)
            {
                continue;
            }

            foreach (var inline in paragraph.Inlines)
            {
                if (inline is Run run && run.Background != null)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static readonly Color MatchHighlightColor = Color.FromRgb(255, 236, 153);
    private static readonly Color FocusHighlightColor = Color.FromRgb(255, 165, 0);

    private static int CountRunsWithBackground(RichTextBox richTextBox, Color color)
    {
        var count = 0;
        foreach (var block in richTextBox.Document.Blocks)
        {
            if (block is not Paragraph paragraph)
            {
                continue;
            }

            foreach (var inline in paragraph.Inlines)
            {
                if (inline is Run run && run.Background is SolidColorBrush brush && brush.Color == color)
                {
                    count++;
                }
            }
        }

        return count;
    }

    private static void InvokeSetProcessedOutput(MainWindow window, IEnumerable<string> lines)
    {
        var method = typeof(MainWindow).GetMethod("SetProcessedOutput", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { lines, true });
    }

    private static void InvokeSetRawOutput(MainWindow window, IEnumerable<string> lines)
    {
        var method = typeof(MainWindow).GetMethod("SetRawOutput", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { lines });
    }

    private static void InvokeRawFind(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod("ExecuteRawFind", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, Array.Empty<object>());
    }

    private static void InvokeProcessedFind(MainWindow window)
    {
        var method = typeof(MainWindow).GetMethod("ExecuteProcessedFind", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, Array.Empty<object>());
    }

    private static void FlushLayout(FrameworkElement element)
    {
        element.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.Loaded);
        element.UpdateLayout();
    }

    private static OracleByFPCLtd.UI.Panels.DiagnosticsPanel GetDiagnostics(MainWindow window)
    {
        return (OracleByFPCLtd.UI.Panels.DiagnosticsPanel)window.FindName("DiagnosticsPanel")!;
    }

    private static RichTextBox GetRawLogTextBox(MainWindow window) =>
        GetDiagnostics(window).RawOutputPanel.LogOutputView.LogTextBox;

    private static TextBox GetRawFindTextBox(MainWindow window) =>
        GetDiagnostics(window).RawOutputPanel.FindBar.FindTextBox;

    private static TextBlock GetRawFindCountText(MainWindow window) =>
        GetDiagnostics(window).RawOutputPanel.FindBar.FindCountText;

    private static Button GetRawFindPrevButton(MainWindow window) =>
        GetDiagnostics(window).RawOutputPanel.FindBar.FindPrevButton;

    private static Button GetRawFindNextButton(MainWindow window) =>
        GetDiagnostics(window).RawOutputPanel.FindBar.FindNextButton;

    private static RichTextBox GetProcessedLogTextBox(MainWindow window) =>
        GetDiagnostics(window).ProcessedOutputPanel.LogOutputView.LogTextBox;

    private static TextBox GetProcessedFindTextBox(MainWindow window) =>
        GetDiagnostics(window).ProcessedOutputPanel.FindBar.FindTextBox;

    private static TextBlock GetProcessedFindCountText(MainWindow window) =>
        GetDiagnostics(window).ProcessedOutputPanel.FindBar.FindCountText;

    private static Button GetProcessedFindPrevButton(MainWindow window) =>
        GetDiagnostics(window).ProcessedOutputPanel.FindBar.FindPrevButton;

    private static Button GetProcessedFindNextButton(MainWindow window) =>
        GetDiagnostics(window).ProcessedOutputPanel.FindBar.FindNextButton;

    private static T? FindVisualChild<T>(DependencyObject root) where T : DependencyObject
    {
        var count = VisualTreeHelper.GetChildrenCount(root);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(root, i);
            if (child is T match)
            {
                return match;
            }

            var nested = FindVisualChild<T>(child);
            if (nested != null)
            {
                return nested;
            }
        }

        return null;
    }

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        using var done = new ManualResetEvent(false);
        var thread = new Thread(() =>
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                failure = ex;
            }
            finally
            {
                done.Set();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        done.WaitOne();
        if (failure != null)
        {
            throw new InvalidOperationException("STA test failed.", failure);
        }
    }
}
