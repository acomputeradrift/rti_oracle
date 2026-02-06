using VariableSubscribeProbe;
using Xunit;

namespace VariableSubscribeProbe.Tests;

public sealed class SubscribePayloadTests
{
    [Theory]
    [InlineData(265, true, "{\"type\":\"Subscribe\",\"resource\":\"Sysvar\",\"value\":{\"id\":265,\"status\":true}}")]
    [InlineData(265, false, "{\"type\":\"Subscribe\",\"resource\":\"Sysvar\",\"value\":{\"id\":265,\"status\":false}}")]
    public void BuildSysvarTogglePayloadUsesExpectedShape(int id, bool status, string expected)
    {
        var payload = SubscriptionRequest.BuildSysvarTogglePayload(id, status);

        Assert.Equal(expected, payload);
    }
}
