using Newtonsoft.Json;

namespace RawAccel.Contracts
{
    // Mirrors AccelArgs in wrapper/wrapper.cpp. JsonProperty names are the
    // contract; do not rename without bumping settings.json compatibility.
    public class RawAccelAccelArgs
    {
        // Maximum number of LUT (input, output) sample pairs the native side
        // accepts; mirrors wrapper.cpp's literal AccelArgs.MaxLutPoints which
        // resolves to ra::LUT_POINTS_CAPACITY.
        public const int MaxLutPoints = RawAccelConstants.LutPointsCapacity;

        public AccelMode mode { get; set; } = AccelMode.noaccel;

        [JsonProperty("Gain / Velocity")]
        public bool gain { get; set; } = true;

        public double inputOffset { get; set; }
        public double outputOffset { get; set; }
        public double acceleration { get; set; } = 0.005;
        public double decayRate { get; set; } = 0.1;
        public double gamma { get; set; } = 1.0;
        public double motivity { get; set; } = 1.5;
        public double exponentClassic { get; set; } = 2.0;
        public double scale { get; set; } = 1.0;
        public double exponentPower { get; set; } = 0.05;
        public double limit { get; set; } = 1.5;
        public double syncSpeed { get; set; } = 5.0;
        public double smooth { get; set; } = 0.5;

        [JsonProperty("Cap / Jump")]
        public Vec2<double> cap { get; set; } = new Vec2<double> { x = 15.0, y = 1.5 };

        [JsonProperty("Cap mode")]
        public CapMode capMode { get; set; } = CapMode.output;

        // length is internal bookkeeping for native marshalling; not in JSON.
        [JsonIgnore]
        public int length { get; set; }

        // Raw LUT data. On the wire this carries only the populated points
        // (length samples); native side resizes to LUT_RAW_DATA_CAPACITY.
        // Defaulted to empty (not null) because wrapper.cpp's OnDeserialized
        // dereferences data->Length without a null check.
        public float[] data { get; set; } = System.Array.Empty<float>();
    }
}
