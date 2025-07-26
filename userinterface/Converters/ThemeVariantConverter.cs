using Avalonia;
using Avalonia.Platform;
using Avalonia.Styling;

namespace userinterface.Converters
{
    public static class ThemeVariantConverter
    {
        /// <summary>
        /// Converts a theme setting string to the actual ThemeVariant, resolving "system" to the OS theme.
        /// </summary>
        /// <param name="themeSetting">The theme setting ("light", "dark", "system")</param>
        /// <returns>The resolved ThemeVariant</returns>
        public static ThemeVariant GetActualTheme(string themeSetting)
        {
            return themeSetting?.ToLower() switch
            {
                "light" => ThemeVariant.Light,
                "dark" => ThemeVariant.Dark,
                "system" or _ => GetSystemThemeVariant()
            };
        }

        /// <summary>
        /// Gets the current system theme variant.
        /// </summary>
        /// <returns>The system's current ThemeVariant</returns>
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