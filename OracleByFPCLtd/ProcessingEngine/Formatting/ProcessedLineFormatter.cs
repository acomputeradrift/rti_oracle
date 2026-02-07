using OracleByFPCLtd.ProcessingEngine.Models;

namespace OracleByFPCLtd.ProcessingEngine.Formatting;

public static class ProcessedLineFormatter
{
    public static string Format(ProcessedLine line)
    {
        return line.Text;
    }
}
