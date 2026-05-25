using Newtonsoft.Json;

namespace RawAccel.Contracts
{
    // Mirrors SpeedArgs in wrapper/wrapper.cpp.
    public class RawAccelSpeedArgs
    {
        [JsonProperty("Whole/combined accel (set false for 'by component' mode)")]
        public bool combineMagnitudes { get; set; } = true;

        public double lpNorm { get; set; } = 2.0;

        [JsonProperty("Time in ms after which an input is weighted at half its original value.")]
        public double inputSmoothHalflife { get; set; }

        [JsonProperty("Time in ms after which scale is weighted at half its original value.")]
        public double scaleSmoothHalflife { get; set; }

        [JsonProperty("Time in ms after which an output is weighted at half its original value.")]
        public double outputSmoothHalflife { get; set; }
    }
}
