using System.Collections.Generic;

namespace RawAccel.Contracts
{
    // Root JSON contract, consumed unchanged by both the Windows wrapper
    // (IOCTL) and Linux agent (socket) paths. These POCOs mirror the
    // wrapper.cpp types; the JsonProperty names are the wire format
    // (settings.json + driver), so renaming a field breaks compatibility.
    public class RawAccelConfig
    {
        public string version { get; set; } = string.Empty;

        public RawAccelDeviceConfig defaultDeviceConfig { get; set; } = new RawAccelDeviceConfig();

        public List<RawAccelProfile> profiles { get; set; } = new List<RawAccelProfile>();

        public List<RawAccelDeviceSettings> devices { get; set; } = new List<RawAccelDeviceSettings>();
    }
}
