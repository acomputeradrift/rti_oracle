namespace OracleByFPCLtd.ExportProcessedLogs.IO;

public interface IExportFileWriter
{
    void Write(string outputPath, byte[] bytes);
}
