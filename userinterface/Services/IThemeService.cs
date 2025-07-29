using SkiaSharp;
using System;

namespace userinterface.Services
{
    public interface IThemeService
    {
        event EventHandler? ThemeChanged;
        void NotifyThemeChanged();
        void ApplyTheme(string themeName);
        void ApplyThemeFromSettings();
        
        SKColor GetCachedColor(string resourceKey);
        void InvalidateColorCache();
    }
}