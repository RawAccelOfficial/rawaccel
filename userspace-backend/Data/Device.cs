using System;
using System.Text.Json.Serialization;

namespace userspace_backend.Data
{
    public class Device
    {
        public string Name { get; set; }

        public string HWID { get; set; }

        public int DPI { get; set; }

        public int PollingRate { get; set; }

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool Ignore { get; set; }

        public string DeviceGroup { get; set; }

        public override bool Equals(object? obj)
        {
            return obj is Device device &&
                   string.Equals(Name, device.Name, StringComparison.OrdinalIgnoreCase) &&
                   string.Equals(HWID, device.HWID, StringComparison.OrdinalIgnoreCase) &&
                   DPI == device.DPI &&
                   PollingRate == device.PollingRate &&
                   Ignore == device.Ignore &&
                   string.Equals(DeviceGroup, device.DeviceGroup, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            HashCode hash = new HashCode();
            hash.Add(Name, StringComparer.OrdinalIgnoreCase);
            hash.Add(HWID, StringComparer.OrdinalIgnoreCase);
            hash.Add(DPI);
            hash.Add(PollingRate);
            hash.Add(Ignore);
            hash.Add(DeviceGroup, StringComparer.OrdinalIgnoreCase);
            return hash.ToHashCode();
        }
    }
}
