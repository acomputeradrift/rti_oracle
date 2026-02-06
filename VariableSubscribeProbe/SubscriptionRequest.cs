using System.Text.Json;

namespace VariableSubscribeProbe;

public static class SubscriptionRequest
{
    public static string BuildSysvarTogglePayload(int id, bool status)
    {
        var payload = new
        {
            type = "Subscribe",
            resource = "Sysvar",
            value = new
            {
                id,
                status
            }
        };

        return JsonSerializer.Serialize(payload);
    }

    public static string BuildSysvarPersSubscribePayload()
    {
        var payload = new
        {
            type = "Subscribe",
            resource = "SysvarPers"
        };

        return JsonSerializer.Serialize(payload);
    }
}
