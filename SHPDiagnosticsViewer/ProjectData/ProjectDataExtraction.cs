using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;

namespace SHPDiagnosticsViewer.ProjectData;

public sealed class ProjectDataExtractionResult
{
    public List<DiagnosticsMappingEntry> DiagnosticsMapping { get; } = new();
    public List<ProjectReportEntry> ProjectReport { get; } = new();
    public List<ProjectTestEntry> ProjectTest { get; } = new();
    public ApexDiscoveryPreloadResult ApexDiscoveryPreload { get; set; } = new();
}

public sealed record DiagnosticsMappingEntry(
    int DeviceId,
    string DeviceName,
    int RtiAddress,
    int PageIndex,
    int PageId,
    int PageNameId,
    string PageName)
{
    public int PageNumber => PageIndex + 1;
}

public sealed record ProjectReportEntry(
    string EntityType,
    int PrimaryId,
    int SecondaryId,
    int TertiaryId,
    string Name,
    string Detail);

public sealed record ProjectTestEntry(
    int DeviceId,
    string DeviceName,
    int RtiAddress,
    int SourceLabelId,
    int SourceLabelIndex,
    string SourceLabelName,
    int PageId,
    int PageNameId,
    string PageName,
    int ButtonId,
    int ButtonTagId,
    string ButtonText);

public sealed record ProjectDataExtractionProgress(string Stage, int Percent);

public interface IProjectDataExtractor
{
    ProjectDataExtractionResult Extract(string apexPath, IProgress<ProjectDataExtractionProgress>? progress = null);
}

public sealed class ProjectDataExtractor : IProjectDataExtractor
{
    public ProjectDataExtractionResult Extract(string apexPath, IProgress<ProjectDataExtractionProgress>? progress = null)
    {
        if (!File.Exists(apexPath))
        {
            throw new FileNotFoundException("APEX file not found.", apexPath);
        }

        var result = new ProjectDataExtractionResult();
        Report(progress, "Opening project database", 5);

        using var connection = new SqliteConnection($"Data Source={apexPath};Mode=ReadOnly");
        connection.Open();

        var devices = LoadDevices(connection);
        var rooms = LoadRooms(connection);
        var ports = LoadPortLabels(connection);
        var pageNames = LoadPageNames(connection);
        var rtiDeviceData = LoadRtiDeviceData(connection);
        var rtiDevicePages = LoadRtiDevicePages(connection);
        var sourceLabels = LoadSourceLabels(connection);
        var layers = LoadLayers(connection);
        var buttons = LoadDeviceButtons(connection);

        Report(progress, "Building diagnostics mapping", 40);
        var deviceById = devices.ToDictionary(entry => entry.DeviceId);
        var pageNameById = pageNames.ToDictionary(entry => entry.PageNameId, entry => entry.PageName);
        var rtiAddressByDeviceId = rtiDeviceData.ToDictionary(entry => entry.DeviceId, entry => entry.RtiAddress);

        foreach (var page in rtiDevicePages)
        {
            foreach (var device in devices)
            {
                if (!rtiAddressByDeviceId.TryGetValue(device.DeviceId, out var rtiAddress))
                {
                    continue;
                }

                if (rtiAddress != page.RtiAddress)
                {
                    continue;
                }

                pageNameById.TryGetValue(page.PageNameId, out var pageName);
                result.DiagnosticsMapping.Add(new DiagnosticsMappingEntry(
                    device.DeviceId,
                    device.Name,
                    rtiAddress,
                    page.PageIndex,
                    page.PageId,
                    page.PageNameId,
                    pageName ?? ""));
            }
        }

        result.DiagnosticsMapping.Sort((left, right) =>
        {
            var deviceCompare = left.DeviceId.CompareTo(right.DeviceId);
            if (deviceCompare != 0)
            {
                return deviceCompare;
            }

            return left.PageIndex.CompareTo(right.PageIndex);
        });

        Report(progress, "Building project report", 65);
        foreach (var room in rooms)
        {
            result.ProjectReport.Add(new ProjectReportEntry(
                "Room",
                room.RoomId,
                room.HomePageId,
                room.RoomOrder,
                room.Name,
                ""));
        }

        foreach (var device in devices)
        {
            result.ProjectReport.Add(new ProjectReportEntry(
                "Device",
                device.DeviceId,
                device.RoomId,
                0,
                device.Name,
                ""));
        }

        foreach (var port in ports)
        {
            result.ProjectReport.Add(new ProjectReportEntry(
                "Port",
                port.PortLabelId,
                port.LabelKey,
                port.RtiAddress,
                port.LabelName,
                ""));
        }

        foreach (var page in rtiDevicePages)
        {
            pageNameById.TryGetValue(page.PageNameId, out var pageName);
            result.ProjectReport.Add(new ProjectReportEntry(
                "Page",
                page.PageId,
                page.PageNameId,
                page.RtiAddress,
                pageName ?? "",
                ""));
        }

        foreach (var source in sourceLabels)
        {
            result.ProjectReport.Add(new ProjectReportEntry(
                "Source",
                source.SourceLabelId,
                source.LabelIndex,
                source.RtiAddress,
                source.LabelName,
                ""));
        }

        result.ProjectReport.Sort((left, right) =>
        {
            var typeCompare = string.Compare(left.EntityType, right.EntityType, StringComparison.Ordinal);
            if (typeCompare != 0)
            {
                return typeCompare;
            }

            var primaryCompare = left.PrimaryId.CompareTo(right.PrimaryId);
            if (primaryCompare != 0)
            {
                return primaryCompare;
            }

            return left.SecondaryId.CompareTo(right.SecondaryId);
        });

        Report(progress, "Building project test index", 85);
        var sourceByAddressAndIndex = sourceLabels.ToDictionary(
            entry => (entry.RtiAddress, entry.LabelIndex),
            entry => entry);
        var layersByPage = layers.GroupBy(entry => entry.PageId).ToDictionary(entry => entry.Key, entry => entry.ToList());
        var buttonsBySharedLayer = buttons.GroupBy(entry => entry.SharedLayerId).ToDictionary(entry => entry.Key, entry => entry.ToList());

        foreach (var device in devices)
        {
            if (!rtiAddressByDeviceId.TryGetValue(device.DeviceId, out var rtiAddress))
            {
                continue;
            }

            var devicePages = rtiDevicePages.Where(entry => entry.RtiAddress == rtiAddress).ToList();
            foreach (var page in devicePages)
            {
                if (!layersByPage.TryGetValue(page.PageId, out var pageLayers))
                {
                    continue;
                }

                pageNameById.TryGetValue(page.PageNameId, out var pageName);
                foreach (var layer in pageLayers)
                {
                    if (layer.SourceId is null)
                    {
                        continue;
                    }

                    if (!sourceByAddressAndIndex.TryGetValue((rtiAddress, layer.SourceId.Value), out var sourceLabel))
                    {
                        continue;
                    }

                    if (!buttonsBySharedLayer.TryGetValue(layer.SharedLayerId, out var layerButtons))
                    {
                        continue;
                    }

                    foreach (var button in layerButtons)
                    {
                        result.ProjectTest.Add(new ProjectTestEntry(
                            device.DeviceId,
                            device.Name,
                            rtiAddress,
                            sourceLabel.SourceLabelId,
                            sourceLabel.LabelIndex,
                            sourceLabel.LabelName,
                            page.PageId,
                            page.PageNameId,
                            pageName ?? "",
                            button.ButtonId,
                            button.ButtonTagId,
                            button.Text));
                    }
                }
            }
        }

        result.ProjectTest.Sort((left, right) =>
        {
            var deviceCompare = left.DeviceId.CompareTo(right.DeviceId);
            if (deviceCompare != 0)
            {
                return deviceCompare;
            }

            var pageCompare = left.PageId.CompareTo(right.PageId);
            if (pageCompare != 0)
            {
                return pageCompare;
            }

            return left.ButtonId.CompareTo(right.ButtonId);
        });

        Report(progress, "Complete", 100);
        result.ApexDiscoveryPreload = ApexDiscoveryPreloadExtractor.Extract(apexPath);
        return result;
    }

    private static void Report(IProgress<ProjectDataExtractionProgress>? progress, string stage, int percent)
    {
        progress?.Report(new ProjectDataExtractionProgress(stage, percent));
    }

    private static List<DeviceRow> LoadDevices(SqliteConnection connection)
    {
        var results = new List<DeviceRow>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DeviceId, RoomId, Name FROM Devices ORDER BY DeviceId";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1))
            {
                continue;
            }

            results.Add(new DeviceRow(reader.GetInt32(0), reader.GetInt32(1), reader.IsDBNull(2) ? "" : reader.GetString(2)));
        }
        return results;
    }

    private static List<RoomRow> LoadRooms(SqliteConnection connection)
    {
        var results = new List<RoomRow>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT RoomId, Name, HomePageId, RoomOrder FROM Rooms ORDER BY RoomId";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(2) || reader.IsDBNull(3))
            {
                continue;
            }

            results.Add(new RoomRow(reader.GetInt32(0), reader.IsDBNull(1) ? "" : reader.GetString(1), reader.GetInt32(2), reader.GetInt32(3)));
        }
        return results;
    }

    private static List<PortLabelRow> LoadPortLabels(SqliteConnection connection)
    {
        var results = new List<PortLabelRow>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT PortLabelId, RTIAddress, LabelKey, LabelName FROM PortLabels ORDER BY PortLabelId";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2))
            {
                continue;
            }

            results.Add(new PortLabelRow(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.IsDBNull(3) ? "" : reader.GetString(3)));
        }
        return results;
    }

    private static List<PageNameRow> LoadPageNames(SqliteConnection connection)
    {
        var results = new List<PageNameRow>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT PageNameId, PageName FROM PageNames ORDER BY PageNameId";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0))
            {
                continue;
            }

            results.Add(new PageNameRow(reader.GetInt32(0), reader.IsDBNull(1) ? "" : reader.GetString(1)));
        }
        return results;
    }

    private static List<RtiDeviceRow> LoadRtiDeviceData(SqliteConnection connection)
    {
        var results = new List<RtiDeviceRow>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT DeviceId, RTIAddress FROM RTIDeviceData ORDER BY DeviceId";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1))
            {
                continue;
            }

            results.Add(new RtiDeviceRow(reader.GetInt32(0), reader.GetInt32(1)));
        }
        return results;
    }

    private static List<RtiDevicePageRow> LoadRtiDevicePages(SqliteConnection connection)
    {
        var results = new List<RtiDevicePageRow>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT RTIAddress, PageId, PageNameId, PageOrder FROM RTIDevicePageData ORDER BY RTIAddress, PageOrder";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2) || reader.IsDBNull(3))
            {
                continue;
            }

            results.Add(new RtiDevicePageRow(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.GetInt32(3)));
        }
        return results;
    }

    private static List<SourceLabelRow> LoadSourceLabels(SqliteConnection connection)
    {
        var results = new List<SourceLabelRow>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT SourceLabelId, RTIAddress, LabelIndex, LabelName FROM SourceLabels ORDER BY SourceLabelId";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2))
            {
                continue;
            }

            results.Add(new SourceLabelRow(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.IsDBNull(3) ? "" : reader.GetString(3)));
        }
        return results;
    }

    private static List<LayerRow> LoadLayers(SqliteConnection connection)
    {
        var results = new List<LayerRow>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT PageId, SharedLayerId, SourceId FROM Layers ORDER BY PageId, LayerId";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1))
            {
                continue;
            }

            results.Add(new LayerRow(reader.GetInt32(0), reader.GetInt32(1), reader.IsDBNull(2) ? (int?)null : reader.GetInt32(2)));
        }
        return results;
    }

    private static List<ButtonRow> LoadDeviceButtons(SqliteConnection connection)
    {
        var results = new List<ButtonRow>();
        using var command = connection.CreateCommand();
        command.CommandText = "SELECT ButtonId, SharedLayerId, ButtonTagId, Text FROM RTIDeviceButtonData ORDER BY ButtonId";
        using var reader = command.ExecuteReader();
        while (reader.Read())
        {
            if (reader.IsDBNull(0) || reader.IsDBNull(1) || reader.IsDBNull(2))
            {
                continue;
            }

            results.Add(new ButtonRow(reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2), reader.IsDBNull(3) ? "" : reader.GetString(3)));
        }
        return results;
    }

    private sealed record DeviceRow(int DeviceId, int RoomId, string Name);
    private sealed record RoomRow(int RoomId, string Name, int HomePageId, int RoomOrder);
    private sealed record PortLabelRow(int PortLabelId, int RtiAddress, int LabelKey, string LabelName);
    private sealed record PageNameRow(int PageNameId, string PageName);
    private sealed record RtiDeviceRow(int DeviceId, int RtiAddress);
    private sealed record RtiDevicePageRow(int RtiAddress, int PageId, int PageNameId, int PageIndex);
    private sealed record SourceLabelRow(int SourceLabelId, int RtiAddress, int LabelIndex, string LabelName);
    private sealed record LayerRow(int PageId, int SharedLayerId, int? SourceId);
    private sealed record ButtonRow(int ButtonId, int SharedLayerId, int ButtonTagId, string Text);
}
