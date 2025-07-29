using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using SkiaSharp;
using System;
using System.Collections.Generic;

namespace userinterface.Services
{
    public class ThemeService : IThemeService
    {
        private readonly ISettingsService settingsService;
        private readonly Dictionary<string, SKColor> colorCache = new();

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
            InvalidateColorCache();
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

        public SKColor GetCachedColor(string resourceKey)
        {
            if (colorCache.TryGetValue(resourceKey, out var cachedColor))
                return cachedColor;

            var color = ResolveThemeColor(resourceKey);
            colorCache[resourceKey] = color;
            return color;
        }

        public void InvalidateColorCache()
        {
            colorCache.Clear();
        }

        private SKColor ResolveThemeColor(string resourceKey)
        {
            var app = Application.Current;
            if (app?.Resources == null || !app.Resources.TryGetResource(resourceKey, app.ActualThemeVariant, out var resource))
                return GetFallbackColor(resourceKey);

            return resource switch
            {
                SolidColorBrush brush => new SKColor(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A),
                ImmutableSolidColorBrush brush => new SKColor(brush.Color.R, brush.Color.G, brush.Color.B, brush.Color.A),
                _ => GetFallbackColor(resourceKey)
            };
        }

        private static SKColor GetFallbackColor(string resourceKey)
        {
            return resourceKey switch
            {
                "PrimaryTextBrush" => SKColors.White,
                "SecondaryTextBrush" => SKColors.LightGray,
                "BorderBrush" => SKColors.Gray,
                "CardBackgroundBrush" => SKColors.Black,
                _ => SKColors.White
            };
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