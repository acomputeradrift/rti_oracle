using System;
using System.Threading;

namespace OracleByFPCLtd.Logging;

public static class LogLevelCommandContext
{
    private static readonly AsyncLocal<string?> CurrentLabel = new();
    private static readonly AsyncLocal<bool> ResendFlag = new();

    public static IDisposable BeginBaseline()
    {
        return Begin("Baseline");
    }

    public static IDisposable BeginResend()
    {
        var previous = ResendFlag.Value;
        ResendFlag.Value = true;
        return new Scope(() => ResendFlag.Value = previous);
    }

    public static string? GetLabel()
    {
        return CurrentLabel.Value;
    }

    public static bool IsResend()
    {
        return ResendFlag.Value;
    }

    private static IDisposable Begin(string label)
    {
        var previous = CurrentLabel.Value;
        CurrentLabel.Value = label;
        return new Scope(() => CurrentLabel.Value = previous);
    }

    private sealed class Scope : IDisposable
    {
        private readonly Action _onDispose;
        private bool _disposed;

        public Scope(Action onDispose)
        {
            _onDispose = onDispose;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            _onDispose();
        }
    }
}
