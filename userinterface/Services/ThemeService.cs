using Avalonia;
using Avalonia.Styling;
using System;

namespace userinterface.Services
{
    public class ThemeService : IThemeService
    {
        private readonly ISettingsService settingsService;

        public ThemeService(ISettingsService settingsService)
        {
            this.settingsService = settingsService;
            
            // Listen for theme changes from settings service
            this.settingsService.ThemeChanged += OnSettingsThemeChanged;
            
            // Apply initial theme
            ApplyThemeFromSettings();
        }

        public event EventHandler? ThemeChanged;

        public void NotifyThemeChanged()
        {
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }

        public void ApplyTheme(string themeName)
        {
            if (Application.Current is null) return;

            ThemeVariant themeVariant = themeName.ToLower() switch
            {
                "light" => ThemeVariant.Light,
                "dark" => ThemeVariant.Dark,
                "system" or _ => ThemeVariant.Default
            };

            Application.Current.RequestedThemeVariant = themeVariant;
            NotifyThemeChanged();
        }

        public void ApplyThemeFromSettings()
        {
            var themeName = settingsService.Theme;
            ApplyTheme(themeName);
        }

        private void OnSettingsThemeChanged(object? sender, EventArgs e)
        {
            ApplyThemeFromSettings();
        }
    }
}