using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using RawAccel.Contracts;

namespace userspace_backend.Driver.Windows
{
    // IRawAccelDriver implementation backed by the C++/CLI wrapper.dll's
    // DriverConfig.Activate() IOCTL path. RawAccelConfig POCO is converted
    // to the wrapper's managed types via JSON round-trip: both sides share
    // identical [JsonProperty] names so Newtonsoft can deserialize one into
    // the other without manual field mapping.
    //
    // ManagedAccel instances are constructed per profile after the
    // round-trip, since wrapper.DriverConfig.accels is [NonSerialized] and
    // wrapper.DriverConfig.Activate() requires accels.Count == profiles.Count.
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
                var native = JsonConvert.DeserializeObject<DriverConfig>(json)
                    ?? throw new InvalidOperationException(
                        "POCO -> wrapper.DriverConfig deserialization returned null");
                native.accels = native.profiles
                    .Select(p => new ManagedAccel(p))
                    .ToList();
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
            DriverConfig.GetDefault().Deactivate();
        }

        public double GetCurrentMouseSpeed()
        {
            // wrapper exposes per-profile speed via SpeedCalculator; the UI
            // gauge reads from that path directly today, so this telemetry
            // hook stays a no-op until consolidated.
            return 0;
        }
    }
}
