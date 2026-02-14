namespace OracleByFPCLtd.Reliability;

public static class FailureCodes
{
    public const string LogLevelDispatchFailed = "LOGLEVEL_DISPATCH_FAILED";
    public const string LogLevelAckTimeout = "LOGLEVEL_ACK_TIMEOUT";
    public const string TransportNotConnected = "TRANSPORT_NOT_CONNECTED";
    public const string DiscoveryFailed = "DISCOVERY_FAILED";
    public const string DriverLoadFailed = "DRIVER_LOAD_FAILED";
    public const string ProjectParseFailed = "PROJECT_PARSE_FAILED";
    public const string ExportFailed = "EXPORT_FAILED";
    public const string SettingsLoadFallback = "SETTINGS_LOAD_FALLBACK";
}
