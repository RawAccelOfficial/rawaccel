using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using RawAccel.Contracts;

namespace userspace_backend.Driver.Windows
{
    public sealed class WindowsRawAccelDriver : IRawAccelDriver
    {
        private readonly ILogger<WindowsRawAccelDriver> logger;

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

        // TODO: plug in mouse speeds from the OS layer.
        public MouseSpeedSample GetCurrentMouseSpeedSample() => MouseSpeedSample.Zero;
    }
}
