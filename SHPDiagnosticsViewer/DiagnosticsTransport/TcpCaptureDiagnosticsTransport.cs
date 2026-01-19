using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SHPDiagnosticsViewer.DiagnosticsTransport;

public sealed class TcpCaptureDiagnosticsTransport : IDiagnosticsTransport
{
    private readonly int _port;
    private readonly bool _sendProbeOnConnect;
    private TcpClient? _client;
    private CancellationTokenSource? _cts;
    private long _bytesRead;
    private long _bytesWritten;
    private int _disconnectLogged;
    private string _disconnectReason = "";
    private List<byte> _byteBuffer = new();
    private long _recordsEmitted;

    public TcpCaptureDiagnosticsTransport(int port = 2113, bool sendProbeOnConnect = false)
    {
        _port = port;
        _sendProbeOnConnect = sendProbeOnConnect;
    }

    public event EventHandler<string>? RawMessageReceived;
    public event EventHandler<string>? TransportInfo;
    public event EventHandler<string>? TransportError;

    public bool IsConnected => _client != null && _client.Connected;

    public Task<List<string>> DiscoverAsync(TimeSpan timeout)
    {
        return Task.FromResult(new List<string>());
    }

    public async Task ConnectAsync(string ip)
    {
        await DisconnectAsync();

        _client = new TcpClient();
        _cts = new CancellationTokenSource();
        _bytesRead = 0;
        _bytesWritten = 0;
        _disconnectLogged = 0;
        _disconnectReason = "";
        _byteBuffer = new List<byte>();
        _recordsEmitted = 0;
        await _client.ConnectAsync(ip, _port);

        EmitInfo($"[info] TCP capture connected to {ip}:{_port}");

        if (_client.Connected)
        {
            if (_sendProbeOnConnect)
            {
                var probe = new byte[] { 0x00 };
                await _client.GetStream().WriteAsync(probe, 0, 1);
                _bytesWritten += 1;
                EmitInfo("[info] TCP probe sent bytes=1");
            }
            else
            {
                EmitInfo("[info] TCP probe disabled bytes=0");
            }
        }

        _ = Task.Run(() => ReceiveLoopAsync(_client, _cts.Token));
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _cts?.Cancel();
        }
        catch
        {
        }

        _byteBuffer.Clear();

        if (_client != null)
        {
            try
            {
                _client.Close();
            }
            catch
            {
            }
            finally
            {
                _client = null;
            }
        }

        LogDisconnect();
        await Task.CompletedTask;
    }

    public Task SendLogLevelAsync(string type, string level)
    {
        EmitInfo("[info] TCP capture does not send log level commands.");
        return Task.CompletedTask;
    }

    public Task<List<DriverInfo>> LoadDriversAsync(string ip)
    {
        return Task.FromResult(new List<DriverInfo>());
    }

    private async Task ReceiveLoopAsync(TcpClient client, CancellationToken token)
    {
        try
        {
            using var stream = client.GetStream();
            var buffer = new byte[4096];
            while (!token.IsCancellationRequested)
            {
                var count = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), token);
                if (count <= 0)
                {
                    break;
                }

                _bytesRead += count;
                AppendBytes(buffer, count);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            _disconnectReason = ex.Message;
            EmitError($"[error] TCP capture error: {ex.Message}");
        }
        finally
        {
            LogDisconnect();
        }
    }

    private void EmitInfo(string message)
    {
        TransportInfo?.Invoke(this, message);
    }

    private void EmitError(string message)
    {
        TransportError?.Invoke(this, message);
    }

    private void AppendBytes(byte[] buffer, int count)
    {
        if (count <= 0)
        {
            return;
        }

        for (var i = 0; i < count; i++)
        {
            _byteBuffer.Add(buffer[i]);
        }
        EmitRecordsFromBytes();
    }

    private void EmitRecordsFromBytes()
    {
        var delimiter = new byte[] { 0x0D, 0x00, 0x0A, 0x00, 0x03, 0x00 };
        while (true)
        {
            var endIndex = FindDelimiter(_byteBuffer, delimiter);
            if (endIndex < 0)
            {
                return;
            }

            var recordLength = endIndex;
            if (recordLength == 0)
            {
                _byteBuffer.RemoveRange(0, endIndex + delimiter.Length);
                RawMessageReceived?.Invoke(this, "");
                _recordsEmitted++;
                continue;
            }

            if (recordLength % 2 == 1)
            {
                return;
            }

            var recordBytes = _byteBuffer.GetRange(0, recordLength).ToArray();
            _byteBuffer.RemoveRange(0, endIndex + delimiter.Length);
            var record = Encoding.Unicode.GetString(recordBytes, 0, recordBytes.Length);
            record = StripControlEdges(record);
            RawMessageReceived?.Invoke(this, record);
            _recordsEmitted++;
        }
    }

    private static int FindDelimiter(List<byte> buffer, byte[] delimiter)
    {
        if (buffer.Count < delimiter.Length)
        {
            return -1;
        }

        for (var i = 0; i <= buffer.Count - delimiter.Length; i++)
        {
            if (i % 2 != 0)
            {
                continue;
            }

            var match = true;
            for (var j = 0; j < delimiter.Length; j++)
            {
                if (buffer[i + j] != delimiter[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                return i;
            }
        }

        return -1;
    }

    private static int FindSecondaryDelimiter(List<byte> buffer, byte[] delimiter)
    {
        if (buffer.Count < delimiter.Length + 4)
        {
            return -1;
        }

        for (var i = 0; i <= buffer.Count - delimiter.Length; i++)
        {
            if (i % 2 != 0)
            {
                continue;
            }

            var match = true;
            for (var j = 0; j < delimiter.Length; j++)
            {
                if (buffer[i + j] != delimiter[j])
                {
                    match = false;
                    break;
                }
            }

            if (!match)
            {
                continue;
            }

            var nextIndex = i + delimiter.Length;
            if (HasHeaderPrefix(buffer, nextIndex))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool HasHeaderPrefix(List<byte> buffer, int index)
    {
        if (index % 2 != 0)
        {
            return false;
        }

        if (index + 2 > buffer.Count)
        {
            return false;
        }

        var available = Math.Min(128, buffer.Count - index);
        if (available < 4)
        {
            return false;
        }

        if (available % 2 == 1)
        {
            available -= 1;
        }

        var window = new byte[available];
        for (var i = 0; i < available; i++)
        {
            window[i] = buffer[index + i];
        }

        var text = Encoding.Unicode.GetString(window, 0, window.Length);
        return MatchesHeaderPrefix(text);
    }

    private static bool MatchesHeaderPrefix(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        var index = 0;
        while (index < text.Length)
        {
            var ch = text[index];
            if (!char.IsControl(ch) && !char.IsWhiteSpace(ch) && ch != '\uFFFD')
            {
                break;
            }
            index++;
        }

        if (index >= text.Length)
        {
            return false;
        }

        if (!IsAsciiAlpha(text[index]))
        {
            return false;
        }

        index += 1;
        while (index < text.Length)
        {
            var ch = text[index];
            if (ch == '/')
            {
                index++;
                break;
            }

            if (!IsAsciiAlnum(ch))
            {
                return false;
            }
            index++;
        }

        const string marker = "Informat:";
        if (index + marker.Length > text.Length)
        {
            return false;
        }

        for (var i = 0; i < marker.Length; i++)
        {
            if (text[index + i] != marker[i])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsAsciiAlpha(char ch)
    {
        return (ch >= 'A' && ch <= 'Z') || (ch >= 'a' && ch <= 'z');
    }

    private static bool IsAsciiAlnum(char ch)
    {
        return IsAsciiAlpha(ch) || (ch >= '0' && ch <= '9');
    }

    private static string StripControlEdges(string record)
    {
        var start = 0;
        var end = record.Length - 1;

        while (start <= end)
        {
            var ch = record[start];
            if (!char.IsControl(ch) && ch != '\uFFFD')
            {
                break;
            }
            start++;
        }

        while (end >= start)
        {
            var ch = record[end];
            if (!char.IsControl(ch) && ch != '\uFFFD')
            {
                break;
            }
            end--;
        }

        if (end < start)
        {
            return "";
        }

        var trimmed = record.Substring(start, end - start + 1);
        return trimmed.TrimEnd();
    }

    private void LogDisconnect()
    {
        if (Interlocked.Exchange(ref _disconnectLogged, 1) != 0)
        {
            return;
        }

        var reason = string.IsNullOrWhiteSpace(_disconnectReason) ? "" : $" error={_disconnectReason}";
        EmitInfo($"[info] TCP capture disconnected bytes_read={_bytesRead} bytes_written={_bytesWritten} records_emitted={_recordsEmitted}{reason}");
    }
}
