using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Windows.Documents;
using System.Windows.Threading;
using OracleByFPCLtd;
using OracleByFPCLtd.UI.Panels;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class VersionDisplayTests
{
    [Fact]
    public void MainWindowTitleIncludesVersion()
    {
        RunOnSta(() =>
        {
            var window = new MainWindow();
            var expected = $"Oracle by FP&C {GetVersionLabel(typeof(MainWindow).Assembly)}";
            Assert.Equal(expected, window.Title);
        });
    }

    [Fact]
    public void AboutWindowShowsVersion()
    {
        RunOnSta(() =>
        {
            var window = new AboutWindow();
            var expected = $"Oracle by FP&C {GetVersionLabel(typeof(AboutWindow).Assembly)}";
            var text = GetAboutVersionText(window);
            Assert.Equal(expected, text);
        });
    }

    [Fact]
    public void AboutWindowVersionIsClickable()
    {
        RunOnSta(() =>
        {
            var window = new AboutWindow();
            var field = typeof(AboutWindow).GetField("VersionLink", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            var hyperlink = field!.GetValue(window) as Hyperlink;
            Assert.NotNull(hyperlink);
            Assert.Equal("_changelog", hyperlink!.Tag);
        });
    }

    [Fact]
    public void AboutWindowCanLoadChangelogForDisplay()
    {
        var method = typeof(AboutWindow).GetMethod("LoadChangelogForDisplay", BindingFlags.Static | BindingFlags.NonPublic);
        Assert.NotNull(method);
        var result = method!.Invoke(null, null) as string;
        Assert.False(string.IsNullOrWhiteSpace(result));
        Assert.Contains("## [1.1]", result, StringComparison.Ordinal);
        Assert.DoesNotContain("All notable changes for RTI Oracle should be recorded in this file.", result, StringComparison.Ordinal);
        Assert.DoesNotContain("## [Unreleased]", result, StringComparison.Ordinal);
    }

    private static string GetAboutVersionText(AboutWindow window)
    {
        var textField = typeof(AboutWindow).GetField("VersionTextBlock", BindingFlags.Instance | BindingFlags.NonPublic);
        var linkField = typeof(AboutWindow).GetField("VersionLink", BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.NotNull(textField);
        Assert.NotNull(linkField);

        var textBlock = (System.Windows.Controls.TextBlock)textField!.GetValue(window)!;
        var hyperlink = (Hyperlink)linkField!.GetValue(window)!;

        var prefixRange = new TextRange(textBlock.ContentStart, hyperlink.ElementStart);
        var linkText = string.Concat(hyperlink.Inlines.OfType<Run>().Select(run => run.Text));
        var composed = $"{prefixRange.Text}{linkText}".Trim();
        return string.Join(" ", composed.Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string GetVersionLabel(Assembly assembly)
    {
        var info = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        if (!string.IsNullOrWhiteSpace(info))
        {
            var sanitized = info.Split('+')[0].Split('-')[0];
            if (Version.TryParse(sanitized, out var parsed))
            {
                return $"v{FormatVersion(parsed)}";
            }

            return $"v{sanitized}";
        }

        var version = assembly.GetName().Version;
        return version == null ? "vunknown" : $"v{FormatVersion(version)}";
    }

    private static string FormatVersion(Version version)
    {
        if (version.Build <= 0 && version.Revision <= 0)
        {
            return $"{version.Major}.{version.Minor}";
        }

        if (version.Revision <= 0)
        {
            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        return $"{version.Major}.{version.Minor}.{version.Build}.{version.Revision}";
    }

    private static void RunOnSta(Action action)
    {
        Exception? failure = null;
        using var done = new ManualResetEvent(false);
        var thread = new Thread(() =>
        {
            var dispatcher = Dispatcher.CurrentDispatcher;
            dispatcher.BeginInvoke(new Action(() =>
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
                    dispatcher.InvokeShutdown();
                }
            }));
            Dispatcher.Run();
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!done.WaitOne(TimeSpan.FromSeconds(15)))
        {
            thread.IsBackground = true;
            throw new TimeoutException("STA test timed out.");
        }
        if (failure != null)
        {
            throw new InvalidOperationException("STA test failed.", failure);
        }
    }
}
