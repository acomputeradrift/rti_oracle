using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class DriverEntryLogLevelMemoryTests
{
    [Fact]
    public void NewDriverEntryStartsWithNoRememberedNonZeroLevel()
    {
        var driver = new MainWindow.DriverEntry(1, "Events Input", "EVENTS_INPUT");

        Assert.Equal(3, driver.SelectedLevel);
        Assert.Equal(0, driver.LastNonZeroLevel);
    }

    [Fact]
    public void SettingNonZeroLevelUpdatesRememberedLevel()
    {
        var driver = new MainWindow.DriverEntry(1, "Events Input", "EVENTS_INPUT");

        driver.SelectedLevel = 2;

        Assert.Equal(2, driver.SelectedLevel);
        Assert.Equal(2, driver.LastNonZeroLevel);
    }

    [Fact]
    public void SettingZeroDoesNotOverwriteRememberedNonZeroLevel()
    {
        var driver = new MainWindow.DriverEntry(1, "Events Input", "EVENTS_INPUT");
        driver.SelectedLevel = 2;

        driver.SelectedLevel = 0;

        Assert.Equal(0, driver.SelectedLevel);
        Assert.Equal(2, driver.LastNonZeroLevel);
    }
}
