namespace RawAccel.Contracts
{
    // Mirrors DeviceSettings in wrapper/wrapper.cpp. The native side enforces
    // length limits via fixed-size buffers; we keep them as plain strings here
    // and let the conversion layer truncate/validate against
    // RawAccelConstants.MaxNameLen / MaxDevIdLen.
    public class RawAccelDeviceSettings
    {
        public string name { get; set; } = string.Empty;

        public string profile { get; set; } = string.Empty;

        public string id { get; set; } = string.Empty;

        public RawAccelDeviceConfig config { get; set; } = new RawAccelDeviceConfig();
    }
}
