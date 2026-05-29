using Avalonia.Threading;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;
using userspace_backend.Driver;

namespace userinterface.Services
{
    // Background poller for the driver's current input-speed telemetry. Transient,
    // so each chart ViewModel owns one. No-op when the driver is null; when the
    // agent is down the driver returns Zero cheaply, so the loop is safe to run.
    public sealed class MouseSpeedPollingService : IDisposable
    {
        // ~30 Hz. The chart animates at ~100 ms, so the indicator only needs to
        // feel live, not frame-perfect. Each poll opens a fresh AF_UNIX socket.
        private const int PollIntervalMs = 33;

        private readonly IRawAccelDriver? driver;
        private readonly ILogger<MouseSpeedPollingService> logger;
        private readonly object gate = new();

        private CancellationTokenSource? cts;
        private Action<MouseSpeedSample>? onSample;

        public MouseSpeedPollingService(
            IRawAccelDriver? driver,
            ILogger<MouseSpeedPollingService> logger)
        {
            this.driver = driver;
            this.logger = logger;
        }

        public bool IsRunning { get; private set; }

        // Begins polling. onSample is always invoked on the UI thread. Idempotent.
        public void Start(Action<MouseSpeedSample> onSample)
        {
            lock (gate)
            {
                if (IsRunning) return;
                if (driver is null) return;

                this.onSample = onSample;
                cts = new CancellationTokenSource();
                IsRunning = true;
                var token = cts.Token;
                _ = Task.Run(() => LoopAsync(token));
            }
        }

        public void Stop()
        {
            lock (gate)
            {
                if (!IsRunning) return;
                IsRunning = false;
                try { cts?.Cancel(); } catch { /* already disposed */ }
                cts?.Dispose();
                cts = null;
                onSample = null;
            }
        }

        private async Task LoopAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                MouseSpeedSample sample = MouseSpeedSample.Zero;
                try
                {
                    sample = driver!.GetCurrentMouseSpeedSample();
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "mouse speed poll failed");
                }

                if (token.IsCancellationRequested) break;

                var cb = onSample;
                if (cb != null)
                {
                    // Post (not Invoke) so the loop never blocks on UI work.
                    Dispatcher.UIThread.Post(() => cb(sample));
                }

                try
                {
                    await Task.Delay(PollIntervalMs, token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }

        public void Dispose() => Stop();
    }
}
