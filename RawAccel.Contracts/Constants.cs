namespace RawAccel.Contracts
{
    // Mirrors common/rawaccel-base.hpp; keep values in sync with the native side.
    public static class RawAccelConstants
    {
        public const int PollRateMin = 125;
        public const int PollRateMax = 8000;

        // NORMALIZED_DPI: the unit curve math uses (counts/ms at 1000 DPI).
        public const double NormalizedDpi = 1000.0;

        public const double DefaultTimeMin = 1000.0 / PollRateMax / 2.0;
        public const double DefaultTimeMax = 100.0;

        public const double WriteDelayMs = 1000.0;

        public const int MaxDevIdLen = 200;
        public const int MaxNameLen = 256;

        public const int LutRawDataCapacity = 514;
        public const int LutPointsCapacity = LutRawDataCapacity / 2;

        public const string SettingsKey = "Driver settings";

        // TODO: make a versioning system to update all of the versions so
        // 1.7.1 issue where driver is still on 1.7.0 doesnt happen again.
        public const int VersionMajor = 1;
        public const int VersionMinor = 7;
        public const int VersionPatch = 0;
        public const string VersionString = "1.7.0";
    }
}
