using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using RawAccel.Contracts;

namespace userspace_backend.Driver.Windows
{
    public sealed class WindowsRawAccelDriver : IRawAccelDriver, IDisposable
    {
        private readonly ILogger<WindowsRawAccelDriver> logger;
        private readonly object listenerGate = new();

        // Lazy: unused paths skip the window + thread.
        // Volatile for EnsureListener's lock-free fast path.
        private volatile RawInputMouseListener? listener;

        // Replayed into the listener for per-device DPI.
        // Volatile: written by Apply, read by EnsureListener under a different lock.
        private volatile RawAccelConfig? lastConfig;

        private volatile bool disposed;

        public WindowsRawAccelDriver(ILogger<WindowsRawAccelDriver>? logger = null)
        {
            this.logger = logger ?? NullLogger<WindowsRawAccelDriver>.Instance;
        }

        public bool IsAvailable
        {
            get
            {
                try
                {
                    VersionHelper.ValidOrThrow();
                    return true;
                }
                catch (Exception ex)
                {
                    logger.LogDebug(ex, "driver version probe failed");
                    return false;
                }
            }
        }

        public bool Apply(RawAccelConfig config)
        {
            try
            {
                var json = JsonConvert.SerializeObject(config);

                var (native, errors) = DriverConfig.Convert(json);
                if (!string.IsNullOrEmpty(errors))
                {
                    logger.LogError("driver rejected settings: {Errors}", errors);
                    return false;
                }
                native.Activate();
                lastConfig = config;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "driver apply failed");
                return false;
            }

            // Driver is already active; don't fail Apply for a listener hiccup.
            try { listener?.UpdateDevices(config); }
            catch (Exception ex) { logger.LogDebug(ex, "listener device update failed after apply"); }

            return true;
        }

        public RawAccelConfig Read()
        {
            var native = DriverConfig.GetActive();
            var json = native.ToJSON();
            return JsonConvert.DeserializeObject<RawAccelConfig>(json)
                ?? throw new InvalidOperationException(
                    "wrapper.DriverConfig -> POCO deserialization returned null");
        }

        public void Deactivate()
        {
            DriverConfig.Deactivate();
        }

        public MouseSpeedSample GetCurrentMouseSpeedSample()
        {
            try
            {
                return EnsureListener().CurrentSample();
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "mouse speed sample failed");
                return MouseSpeedSample.Zero;
            }
        }

        private RawInputMouseListener EnsureListener()
        {
            var existing = listener;
            if (existing != null) return existing;

            lock (listenerGate)
            {
                if (disposed)
                    throw new ObjectDisposedException(nameof(WindowsRawAccelDriver));
                if (listener == null)
                {
                    var created = new RawInputMouseListener(logger);
                    created.Start();
                    if (lastConfig != null) created.UpdateDevices(lastConfig);
                    listener = created;
                }
                return listener;
            }
        }

        public void Dispose()
        {
            RawInputMouseListener? toDispose;
            lock (listenerGate)
            {
                if (disposed) return;
                disposed = true;
                toDispose = listener;
                listener = null;
            }
            // Outside the lock so thread-join can't block EnsureListener.
            toDispose?.Dispose();
        }
    }
}
