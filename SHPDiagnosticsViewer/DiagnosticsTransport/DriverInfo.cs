namespace SHPDiagnosticsViewer.DiagnosticsTransport;

public sealed class DriverInfo
{
    public DriverInfo(int id, string name, string dName)
    {
        Id = id;
        Name = name;
        DName = dName;
    }

    public int Id { get; }
    public string Name { get; }
    public string DName { get; }
}
