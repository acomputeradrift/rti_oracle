using System;

namespace OracleByFPCLtd.Reliability;

public sealed record OperationFailure(
    string Code,
    string Message,
    string Context,
    DateTime TimestampUtc);
