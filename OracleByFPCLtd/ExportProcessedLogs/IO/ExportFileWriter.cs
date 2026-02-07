using System.IO;

namespace OracleByFPCLtd.ExportProcessedLogs.IO;

public sealed class ExportFileWriter : IExportFileWriter
{
    public void Write(string outputPath, byte[] bytes)
    {
        File.WriteAllBytes(outputPath, bytes);
    }
}
