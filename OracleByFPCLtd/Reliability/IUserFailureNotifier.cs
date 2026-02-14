namespace OracleByFPCLtd.Reliability;

public interface IUserFailureNotifier
{
    void ShowBlockingFailure(string feature, OperationFailure failure);
    void AppendOperationalLog(OperationFailure failure);
    void AppendOperationalResult(string code, string status, string message, string context);
}
