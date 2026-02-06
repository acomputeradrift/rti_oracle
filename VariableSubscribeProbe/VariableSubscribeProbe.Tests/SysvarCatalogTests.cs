using System.Linq;
using VariableSubscribeProbe;
using Xunit;

namespace VariableSubscribeProbe.Tests;

public sealed class SysvarCatalogTests
{
    [Fact]
    public void ParseFromJsonExtractsDriversAndVariables()
    {
        var json = "{" +
                   "\"Drivers\":[" +
                   "{\"Driver Name\":\"Audio Matrix\",\"Driver Base Name\":\"RTI Virtual Multiroom Amp\",\"Driver ID\":0,\"Driver Variables\":[" +
                   "{\"Name\":\"Room One\\\\Source In Use\",\"ID\":260,\"Type\":1}," +
                   "{\"Name\":\"Room One\\\\Volume\",\"ID\":264,\"Type\":1}" +
                   "]}," +
                   "{\"Driver Name\":\"Video Matrix\",\"Driver Base Name\":\"RTI Virtual HDMI Matrix\",\"Driver ID\":1,\"Driver Variables\":[" +
                   "{\"Name\":\"Output 1\\\\Input In Use\",\"ID\":285,\"Type\":1}" +
                   "]}" +
                   "]}";

        var catalog = SysvarCatalog.ParseFromJson(json);

        Assert.Equal(2, catalog.Drivers.Count);

        var audio = catalog.Drivers.Single(d => d.DriverName == "Audio Matrix");
        Assert.Equal("RTI Virtual Multiroom Amp", audio.DriverBaseName);
        Assert.Equal(0, audio.DriverId);
        Assert.Equal(2, audio.Variables.Count);

        var first = audio.Variables.Single(v => v.Id == 260);
        Assert.Equal("Room One\\Source In Use", first.Name);
        Assert.Equal(1, first.Type);

        var video = catalog.Drivers.Single(d => d.DriverName == "Video Matrix");
        Assert.Equal(1, video.Variables.Count);
    }
}
