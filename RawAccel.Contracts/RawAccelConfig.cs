using System.Collections.Generic;

namespace RawAccel.Contracts
{
    // Root JSON contract. Mirrors DriverConfig in wrapper/wrapper.cpp.
    // Both the Windows wrapper IOCTL path and the Linux agent unix-socket
    // path consume this exact shape; do not introduce divergent fields.
    public class RawAccelConfig
    {
        public string version { get; set; } = string.Empty;

        public RawAccelDeviceConfig defaultDeviceConfig { get; set; } = new RawAccelDeviceConfig();

        public List<RawAccelProfile> profiles { get; set; } = new List<RawAccelProfile>();

        public List<RawAccelDeviceSettings> devices { get; set; } = new List<RawAccelDeviceSettings>();
    }
}
