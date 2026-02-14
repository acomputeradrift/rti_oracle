namespace OracleByFPCLtd.Reliability;

public sealed record FeatureOperation(
    string Feature,
    string Target,
    string RequestedValue,
    OperationStatus Status,
    int RetryCount,
    OperationFailure? LastError);
