using Newtonsoft.Json;

namespace RawAccel.Contracts
{
    // Mirrors Profile in wrapper.cpp. Relaxed here (partial JSON tolerated);
    // the native side does the strict all-fields-present validation.
    public class RawAccelProfile
    {
        public string name { get; set; } = "default";

        [JsonProperty("Stretches domain for horizontal vs vertical inputs")]
        public Vec2<double> domainXY { get; set; } = new Vec2<double> { x = 1.0, y = 1.0 };

        [JsonProperty("Stretches accel range for horizontal vs vertical inputs")]
        public Vec2<double> rangeXY { get; set; } = new Vec2<double> { x = 1.0, y = 1.0 };

        [JsonProperty("Whole or horizontal accel parameters")]
        public RawAccelAccelArgs argsX { get; set; } = new RawAccelAccelArgs();

        [JsonProperty("Vertical accel parameters")]
        public RawAccelAccelArgs argsY { get; set; } = new RawAccelAccelArgs();

        [JsonProperty("Input speed calculation parameters")]
        public RawAccelSpeedArgs inputSpeedArgs { get; set; } = new RawAccelSpeedArgs();

        [JsonProperty("Output DPI")]
        public double outputDPI { get; set; } = 1000.0;

        [JsonProperty("Y/X output DPI ratio (vertical sens multiplier)")]
        public double yxOutputDPIRatio { get; set; } = 1.0;

        [JsonProperty("L/R output DPI ratio (left sens multiplier)")]
        public double lrOutputDPIRatio { get; set; } = 1.0;

        [JsonProperty("U/D output DPI ratio (up sens multiplier)")]
        public double udOutputDPIRatio { get; set; } = 1.0;

        [JsonProperty("Degrees of rotation")]
        public double rotation { get; set; }

        [JsonProperty("Degrees of angle snapping")]
        public double snap { get; set; }

        [JsonIgnore]
        public double minimumSpeed { get; set; }

        [JsonProperty("Input Speed Cap")]
        public double maximumSpeed { get; set; }
    }
}
