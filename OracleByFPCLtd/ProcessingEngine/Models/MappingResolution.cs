namespace OracleByFPCLtd.ProcessingEngine.Models;

public sealed record MappingResolution(
    string Kind,
    string MappedFrom,
    string MappedTo,
    string Source,
    string? Profile = null,
    string? Driver = null,
    string? Device = null);
