using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;

namespace userinterface.Converters
{
    public static class ThemeVariantConverter
    {
        public static ThemeVariant GetActualTheme(string themeSetting)
        {
            return themeSetting?.ToLower() switch
            {
                "light" => ThemeVariant.Light,
                "dark" => ThemeVariant.Dark,
                "system" or _ => GetSystemThemeVariant()
            };
        }

        public static ThemeVariant GetSystemThemeVariant()
        {
            var platformTheme = Application.Current?.PlatformSettings?.GetColorValues().ThemeVariant;
            return platformTheme switch
            {
                PlatformThemeVariant.Dark => ThemeVariant.Dark,
                PlatformThemeVariant.Light => ThemeVariant.Light,
                _ => ThemeVariant.Light
            };
        }
    }
}