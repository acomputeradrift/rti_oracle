using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OracleByFPCLtd.DiagnosticsTransport;
using OracleByFPCLtd.DiagnosticsTransport.Connection;
using OracleByFPCLtd.DiagnosticsTransport.Controls;
using OracleByFPCLtd.DiagnosticsTransport.Messaging;
using OracleByFPCLtd.Reliability;
using Xunit;

namespace OracleByFPCLtd.Tests;

public sealed class DiagnosticsTransportFacadeTests
{
    [Fact]
    public async Task DiscoverAsyncDelegatesToConnectionManager()
    {
        var connection = new FakeConnectionManager
        {
            DiscoverResults = new List<string> { "192.168.1.10" }
        };
        var facade = new DiagnosticsTransportFacade(
            connection,
            new FakeMessageReceiver(),
            new FakeLogLevelController(),
            new FakeSysvarSubscriptionController());

        var result = await facade.DiscoverAsync(TimeSpan.FromSeconds(1));

        Assert.Equal(connection.DiscoverResults, result);
        Assert.Equal(1, connection.DiscoverCalls);
        Assert.Equal(TimeSpan.FromSeconds(1), connection.LastDiscoverTimeout);
    }

    [Fact]
    public async Task ConnectAsyncDelegatesToConnectionManager()
    {
        var connection = new FakeConnectionManager();
        var facade = new DiagnosticsTransportFacade(
            connection,
            new FakeMessageReceiver(),
            new FakeLogLevelController(),
            new FakeSysvarSubscriptionController());

        await facade.ConnectAsync("10.0.0.5");

        Assert.Equal(1, connection.ConnectCalls);
        Assert.Equal("10.0.0.5", connection.LastConnectIp);
    }

    [Fact]
    public async Task DisconnectAsyncDelegatesToConnectionManager()
    {
        var connection = new FakeConnectionManager();
        var facade = new DiagnosticsTransportFacade(
            connection,
            new FakeMessageReceiver(),
            new FakeLogLevelController(),
            new FakeSysvarSubscriptionController());

        await facade.DisconnectAsync();

        Assert.Equal(1, connection.DisconnectCalls);
    }

    [Fact]
    public async Task SendLogLevelAsyncDelegatesToController()
    {
        var logLevelController = new FakeLogLevelController();
        var facade = new DiagnosticsTransportFacade(
            new FakeConnectionManager(),
            new FakeMessageReceiver(),
            logLevelController,
            new FakeSysvarSubscriptionController());

        await facade.SendLogLevelAsync("Driver", "Debug");

        Assert.Equal(1, logLevelController.SendCalls);
        Assert.Equal("Driver", logLevelController.LastType);
        Assert.Equal("Debug", logLevelController.LastLevel);
    }

    [Fact]
    public async Task SendLogLevelCommandAsyncDelegatesToController()
    {
        var logLevelController = new FakeLogLevelController();
        var facade = new DiagnosticsTransportFacade(
            new FakeConnectionManager(),
            new FakeMessageReceiver(),
            logLevelController,
            new FakeSysvarSubscriptionController());

        var result = await facade.SendLogLevelCommandAsync("DRIVER//2", "1");

        Assert.True(result.Dispatched);
        Assert.Equal("DRIVER//2", logLevelController.LastType);
        Assert.Equal("1", logLevelController.LastLevel);
    }

    [Fact]
    public async Task LoadDriversAsyncDelegatesToConnectionManager()
    {
        var connection = new FakeConnectionManager
        {
            DriverResults = new List<DriverInfo> { new DriverInfo(1, "Driver 1", "DRIVER//1") }
        };
        var facade = new DiagnosticsTransportFacade(
            connection,
            new FakeMessageReceiver(),
            new FakeLogLevelController(),
            new FakeSysvarSubscriptionController());

        var result = await facade.LoadDriversAsync("10.0.0.5");

        Assert.Equal(connection.DriverResults, result);
        Assert.Equal(1, connection.LoadDriversCalls);
        Assert.Equal("10.0.0.5", connection.LastLoadDriversIp);
    }

    [Fact]
    public void EventsAreForwardedFromComponents()
    {
        var connection = new FakeConnectionManager();
        var receiver = new FakeMessageReceiver();
        var facade = new DiagnosticsTransportFacade(
            connection,
            receiver,
            new FakeLogLevelController(),
            new FakeSysvarSubscriptionController());

        string? info = null;
        string? error = null;
        string? raw = null;
        facade.TransportInfo += (_, message) => info = message;
        facade.TransportError += (_, message) => error = message;
        facade.RawMessageReceived += (_, message) => raw = message;

        connection.RaiseInfo("[info] Ready");
        connection.RaiseError("[error] Disconnected");
        receiver.RaiseRaw("raw message");

        Assert.Equal("[info] Ready", info);
        Assert.Equal("[error] Disconnected", error);
        Assert.Equal("raw message", raw);
    }

    [Fact]
    public void IsConnectedReflectsConnectionManager()
    {
        var connection = new FakeConnectionManager { IsConnected = true };
        var facade = new DiagnosticsTransportFacade(
            connection,
            new FakeMessageReceiver(),
            new FakeLogLevelController(),
            new FakeSysvarSubscriptionController());

        Assert.True(facade.IsConnected);
    }

    private sealed class FakeConnectionManager : IConnectionManager
    {
        public event EventHandler<string>? TransportInfo;
        public event EventHandler<string>? TransportError;

        public bool IsConnected { get; set; }
        public int DiscoverCalls { get; private set; }
        public TimeSpan LastDiscoverTimeout { get; private set; }
        public List<string> DiscoverResults { get; set; } = new();
        public int ConnectCalls { get; private set; }
        public string LastConnectIp { get; private set; } = "";
        public int DisconnectCalls { get; private set; }
        public int LoadDriversCalls { get; private set; }
        public string LastLoadDriversIp { get; private set; } = "";
        public List<DriverInfo> DriverResults { get; set; } = new();

        public Task<List<string>> DiscoverAsync(TimeSpan timeout)
        {
            DiscoverCalls++;
            LastDiscoverTimeout = timeout;
            return Task.FromResult(DiscoverResults);
        }

        public Task ConnectAsync(string ip)
        {
            ConnectCalls++;
            LastConnectIp = ip;
            return Task.CompletedTask;
        }

        public Task DisconnectAsync()
        {
            DisconnectCalls++;
            return Task.CompletedTask;
        }

        public Task<List<DriverInfo>> LoadDriversAsync(string ip)
        {
            LoadDriversCalls++;
            LastLoadDriversIp = ip;
            return Task.FromResult(DriverResults);
        }

        public void RaiseInfo(string message) => TransportInfo?.Invoke(this, message);

        public void RaiseError(string message) => TransportError?.Invoke(this, message);
    }

    private sealed class FakeMessageReceiver : IMessageReceiver
    {
        public event EventHandler<string>? RawMessageReceived;

        public void RaiseRaw(string message) => RawMessageReceived?.Invoke(this, message);
    }

    private sealed class FakeLogLevelController : ILogLevelController
    {
        public event EventHandler<FeatureOperation>? OperationStateChanged;
        public int SendCalls { get; private set; }
        public string LastType { get; private set; } = "";
        public string LastLevel { get; private set; } = "";

        public Task<CommandDispatchResult> SendLogLevelCommandAsync(string type, string level, CancellationToken token = default)
        {
            SendCalls++;
            LastType = type;
            LastLevel = level;
            return Task.FromResult(CommandDispatchResult.Success());
        }

        public Task SendLogLevelAsync(string type, string level)
        {
            SendCalls++;
            LastType = type;
            LastLevel = level;
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSysvarSubscriptionController : ISysvarSubscriptionController
    {
        public Task SendSubscribeAsync(string resource, string value)
        {
            return Task.CompletedTask;
        }
    }
}
