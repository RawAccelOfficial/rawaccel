namespace RawAccel.Contracts
{
    // Mirrors common/rawaccel-base.hpp. Values must stay in sync with the
    // native side; see common/rawaccel-base.hpp and wrapper/wrapper.cpp.
    public static class RawAccelConstants
    {
        public const int PollRateMin = 125;
        public const int PollRateMax = 8000;

        public const double DefaultTimeMin = 1000.0 / PollRateMax / 2.0;
        public const double DefaultTimeMax = 100.0;

        public const double WriteDelayMs = 1000.0;

        public const int MaxDevIdLen = 200;
        public const int MaxNameLen = 256;

        public const int LutRawDataCapacity = 514;
        public const int LutPointsCapacity = LutRawDataCapacity / 2;

        public const string SettingsKey = "Driver settings";

        // Mirrors RA_VER_* in common/rawaccel-version.h. Bump when the
        // settings shape or wire protocol changes incompatibly.
        public const int VersionMajor = 1;
        public const int VersionMinor = 7;
        public const int VersionPatch = 0;
        public const string VersionString = "1.7.0";
    }
}
