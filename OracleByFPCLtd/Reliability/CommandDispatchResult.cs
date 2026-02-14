namespace OracleByFPCLtd.Reliability;

public sealed record CommandDispatchResult(bool Dispatched, OperationFailure? Failure)
{
    public static CommandDispatchResult Success() => new(true, null);

    public static CommandDispatchResult Fail(OperationFailure failure) => new(false, failure);
}
