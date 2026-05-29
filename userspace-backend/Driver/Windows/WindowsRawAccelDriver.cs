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

        // Speed-line capture, created lazily on first poll so unused paths never
        // spin up a window + thread.
        private RawInputMouseListener? listener;

        // Last applied config, replayed into the listener for per-device DPI.
        private RawAccelConfig? lastConfig;

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
                if (errors != null)
                {
                    logger.LogError("driver rejected settings: {Errors}", errors);
                    return false;
                }
                native.Activate();

                lastConfig = config;
                listener?.UpdateDevices(config);

                return true;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "driver apply failed");
                return false;
            }
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
            listener?.Dispose();
            listener = null;
        }
    }
}
