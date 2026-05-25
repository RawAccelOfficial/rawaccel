using Newtonsoft.Json;

namespace RawAccel.Contracts
{
    // Mirrors DeviceConfig in wrapper/wrapper.cpp. ShouldSerialize* methods
    // hide default-valued fields so the JSON stays compact and matches what
    // the wrapper produces today.
    public class RawAccelDeviceConfig
    {
        public bool disable { get; set; }

        public bool setExtraInfo { get; set; }

        [JsonProperty("Use constant time interval based on polling rate")]
        public bool pollTimeLock { get; set; }

        [JsonProperty("DPI (normalizes input speed unit: counts/ms -> in/s)")]
        public int dpi { get; set; } = 1000;

        [JsonProperty("Polling rate Hz (keep at 0 for automatic adjustment)")]
        public int pollingRate { get; set; }

        public double minimumTime { get; set; } = RawAccelConstants.DefaultTimeMin;

        public double maximumTime { get; set; } = RawAccelConstants.DefaultTimeMax;

        public bool ShouldSerializesetExtraInfo() => setExtraInfo;

        public bool ShouldSerializeminimumTime() => minimumTime != RawAccelConstants.DefaultTimeMin;

        public bool ShouldSerializemaximumTime() => maximumTime != RawAccelConstants.DefaultTimeMax;
    }
}
