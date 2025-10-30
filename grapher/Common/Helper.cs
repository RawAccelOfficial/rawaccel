using System;
using System.IO;

namespace grapher.Common
{
    public static class Helper
    {
        public static string GetCfgPath() => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "rawaccel");

        public static string GetDefaultSettingsFilePath() => Path.Combine(
            GetCfgPath(), @"settings.json");

        public static double GetSensitivityFactor(Profile profile) => GetSensitivityFactor(profile.outputDPI);

        public static double GetSensitivityFactor(double outputDPI) => outputDPI / Constants.DriverNormalizedDPI;

        public static double CalculatOutputDPI(double sensitivity) => sensitivity * Constants.DriverNormalizedDPI;
    }
}
