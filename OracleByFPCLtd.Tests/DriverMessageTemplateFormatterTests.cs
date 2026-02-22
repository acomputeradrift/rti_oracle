using OracleByFPCLtd.DriverProfiles.Services;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class DriverMessageTemplateFormatterTests
{
    [Fact]
    public void TryFormatDriverCommandFormatsClipsalImmediateSwitch()
    {
        var mappedText = "[2026-02-10 19:00:39.485] Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(On, Garage Motion Sensor, 56)' Sustain:NO";

        var formatted = DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, "Clipsal C-Bus", out var output);

        Assert.True(formatted);
        Assert.Equal("[2026-02-10 19:00:39.485] Driver Command (Clipsal C-Bus): 'Garage Motion Sensor switched to On.'", output);
    }

    [Fact]
    public void TryFormatDriverCommandFormatsAvproCecHex()
    {
        var mappedText = "[2026-02-10 21:29:42.669] Driver - Command:'AVProEdge MXNet_1G\\CEC\\CEC (Hex)(1, 4F821000)' Sustain:NO";

        var formatted = DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, "AVProEdge MXNet_1G", out var output);

        Assert.True(formatted);
        Assert.Equal("[2026-02-10 21:29:42.669] Driver Command (AVProEdge MXNet_1G): 'CEC hex command 4F821000 send to 1.'", output);
    }

    [Fact]
    public void TryFormatDriverCommandKeepsNoFormatForSystemManagerRouteCommands()
    {
        var mappedText = "[2026-02-10 19:15:33.701] Driver - Command:'System Manager\\[Hide]\\Route Command(2, 1, 3)' Sustain:NO";

        var formatted = DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, "System Manager", out var output);

        Assert.True(formatted);
        Assert.Equal("[2026-02-10 19:15:33.701] Driver Command (System Manager): 'System Manager\\[Hide]\\Route Command(2, 1, 3)' [No Format!]", output);
    }

    [Fact]
    public void TryFormatDriverCommandFormatsSystemManagerLayerVisibility()
    {
        var mappedText = "[2026-02-11 13:50:23.207] Driver - Command:'System Manager\\Layer Visibility\\Set Layer Visibility(Toggle Room List)' Sustain:NO";

        var formatted = DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, "System Manager", out var output);

        Assert.True(formatted);
        Assert.Equal("[2026-02-11 13:50:23.207] Driver Command (System Manager): 'Layer Visibility set to Toggle Room List.'", output);
    }

    [Fact]
    public void TryFormatDriverCommandFormatsSystemManagerHiddenSystemOff()
    {
        var mappedText = "[2026-02-11 14:04:20.430] Driver - Command:'System Manager\\[Hide]\\System Off' Sustain:NO";

        var formatted = DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, "System Manager", out var output);

        Assert.True(formatted);
        Assert.Equal("[2026-02-11 14:04:20.430] Driver Command (System Manager): 'System set to Off.'", output);
    }

    [Fact]
    public void TryFormatDriverCommandFormatsRtiVirtualMultiroomAmpPower()
    {
        var mappedText = "[2026-02-11 13:54:57.482] Driver - Command:'RTI Virtual Multiroom Amp\\Room Three\\Power(On)' Sustain:NO";

        var formatted = DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, "RTI Virtual Multiroom Amp", out var output);

        Assert.True(formatted);
        Assert.Equal("[2026-02-11 13:54:57.482] Driver Command (RTI Virtual Multiroom Amp): 'Room Three power set to On.'", output);
    }

    [Fact]
    public void TryFormatDriverCommandFormatsRtiVirtualMultiroomAmpPowerToggleAsAction()
    {
        var mappedText = "[2026-02-11 13:55:38.601] Driver - Command:'RTI Virtual Multiroom Amp\\Room One\\Power(Toggle)' Sustain:NO";

        var formatted = DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, "RTI Virtual Multiroom Amp", out var output);

        Assert.True(formatted);
        Assert.Equal("[2026-02-11 13:55:38.601] Driver Command (RTI Virtual Multiroom Amp): 'Room One power toggled.'", output);
    }

    [Fact]
    public void TryFormatDriverCommandFormatsYamahaMuteToggleAsAction()
    {
        var mappedText = "[2026-02-11 14:00:00.000] Driver - Command:'Yamaha AVENTAGE\\Main Zone\\Main Mute(Toggle)' Sustain:NO";

        var formatted = DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, "Yamaha AVENTAGE", out var output);

        Assert.True(formatted);
        Assert.Equal("[2026-02-11 14:00:00.000] Driver Command (Yamaha AVENTAGE): 'Main mute toggled.'", output);
    }

    [Fact]
    public void TryFormatDriverCommandFormatsVauxMuteToggleAsAction()
    {
        var mappedText = "[2026-02-11 14:00:01.000] Driver - Command:'Vaux Lattis Matrix\\Output Settings\\Output Mute(Toggle, Gym)' Sustain:NO";

        var formatted = DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, "Vaux Lattis Matrix", out var output);

        Assert.True(formatted);
        Assert.Equal("[2026-02-11 14:00:01.000] Driver Command (Vaux Lattis Matrix): 'Gym mute toggled.'", output);
    }

    [Fact]
    public void TryFormatDriverCommandReturnsFalseWithoutTimestamp()
    {
        var mappedText = "Driver - Command:'Clipsal C-Bus\\General\\Immediate Switch(On, Garage Motion Sensor, 56)' Sustain:NO";

        var formatted = DriverMessageTemplateFormatter.TryFormatDriverCommand(mappedText, "Clipsal C-Bus", out _);

        Assert.False(formatted);
    }

    [Fact]
    public void TryFormatDriverEventFormatsDscPowerSeriesZoneOpen()
    {
        var mappedText = "[2026-02-21 10:12:44.112] Driver event 'When 'Garage West DOOR Opened' happens on 'DSC PowerSeries\\Zone Open''";

        var formatted = DriverMessageTemplateFormatter.TryFormatDriverEvent(mappedText, "DSC PowerSeries", out var output);

        Assert.True(formatted);
        Assert.Equal("[2026-02-21 10:12:44.112] Driver Event (DSC PowerSeries): 'Garage West DOOR Opened.'", output);
    }

    [Fact]
    public void TryFormatDriverEventFormatsVenstarColorTouchOperatingStateChange()
    {
        var mappedText = "[2026-02-21 10:14:44.112] Driver event 'When 'Garage (Stat 2) - Operating State Change' happens on 'Venstar ColorTouch\\Garage (Stat 2) Events''";

        var formatted = DriverMessageTemplateFormatter.TryFormatDriverEvent(mappedText, "Venstar ColorTouch", out var output);

        Assert.True(formatted);
        Assert.Equal("[2026-02-21 10:14:44.112] Driver Event (Venstar ColorTouch): 'Garage (Stat 2) - Operating State Change.'", output);
    }
}
