namespace RawAccel.Contracts
{
    // Mirrors DeviceSettings in wrapper.cpp. Length limits (MaxNameLen /
    // MaxDevIdLen) are enforced by the conversion layer, not here.
    public class RawAccelDeviceSettings
    {
        public string name { get; set; } = string.Empty;

        public string profile { get; set; } = string.Empty;

        public string id { get; set; } = string.Empty;

        public RawAccelDeviceConfig config { get; set; } = new RawAccelDeviceConfig();
    }
}
