using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Xunit;

namespace SHPDiagnosticsViewer.Tests;

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

            var rawLog = (TextBox)window.FindName("RawLogTextBox")!;
            var rawFind = (TextBox)window.FindName("RawFindTextBox")!;
            var countText = (TextBlock)window.FindName("RawFindCountText")!;
            var prevButton = (Button)window.FindName("RawFindPrevButton")!;
            var nextButton = (Button)window.FindName("RawFindNextButton")!;

            var lines = BuildLines(40, 20, 30);
            rawLog.Text = string.Join(Environment.NewLine, lines);
            rawFind.Text = "match";
            InvokeRawFind(window);
            FlushLayout(window);

            Assert.Equal("Match: 1/2", countText.Text);
            Assert.True(prevButton.IsEnabled);
            Assert.True(nextButton.IsEnabled);
            Assert.Equal("match", rawLog.SelectedText);

            var firstMatchLine = GetFirstMatchLineIndex(lines);
            AssertLineIsVisible(rawLog, firstMatchLine);

            nextButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            FlushLayout(window);

            var secondMatchLine = GetSecondMatchLineIndex(lines);
            Assert.Equal("match", rawLog.SelectedText);
            AssertLineIsVisible(rawLog, secondMatchLine);
            Assert.Equal("Match: 2/2", countText.Text);

            prevButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            FlushLayout(window);

            Assert.Equal("match", rawLog.SelectedText);
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

            var processedLog = (RichTextBox)window.FindName("ProcessedLogTextBox")!;
            var processedFind = (TextBox)window.FindName("ProcessedFindTextBox")!;
            var countText = (TextBlock)window.FindName("ProcessedFindCountText")!;
            var prevButton = (Button)window.FindName("ProcessedFindPrevButton")!;
            var nextButton = (Button)window.FindName("ProcessedFindNextButton")!;

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

    private static void AssertLineIsVisible(TextBox textBox, int expectedLineIndex)
    {
        var selectionLine = textBox.GetLineIndexFromCharacterIndex(textBox.SelectionStart);
        Assert.Equal(expectedLineIndex, selectionLine);

        var firstVisible = textBox.GetFirstVisibleLineIndex();
        var lastVisible = textBox.GetLastVisibleLineIndex();
        Assert.True(firstVisible >= 0);
        Assert.True(lastVisible >= firstVisible);

        Assert.InRange(selectionLine, firstVisible, lastVisible);
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

            var rawLog = (TextBox)window.FindName("RawLogTextBox")!;
            var rawFind = (TextBox)window.FindName("RawFindTextBox")!;
            var countText = (TextBlock)window.FindName("RawFindCountText")!;
            var prevButton = (Button)window.FindName("RawFindPrevButton")!;
            var nextButton = (Button)window.FindName("RawFindNextButton")!;

            rawLog.Text = "No entries here";
            rawFind.Text = "absent";
            InvokeRawFind(window);
            FlushLayout(window);

            Assert.Equal("Match: None", countText.Text);
            Assert.False(prevButton.IsEnabled);
            Assert.False(nextButton.IsEnabled);

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

    private static void InvokeSetProcessedOutput(MainWindow window, IEnumerable<string> lines)
    {
        var method = typeof(MainWindow).GetMethod("SetProcessedOutput", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(method);
        method!.Invoke(window, new object[] { lines, true });
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
