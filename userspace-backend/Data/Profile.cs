using System.Text.Json.Serialization;
using userspace_backend.Data.Profiles;

namespace userspace_backend.Data
{
    public class Profile
    {
        [JsonRequired]
        public string Name { get; set; }

        public int OutputDPI { get; set; }

        public double YXRatio { get; set; }

        [JsonRequired]
        public Acceleration Acceleration { get; set; }

        [JsonRequired]
        public Hidden Hidden { get; set; }
    }
}
