using System.Threading.Tasks;

namespace OracleByFPCLtd.DiagnosticsTransport.Controls;

public interface ILogLevelController
{
    Task SendLogLevelAsync(string type, string level);
}
