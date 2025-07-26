using System;

namespace userinterface.Services
{
    public interface IThemeService
    {
        event EventHandler? ThemeChanged;
        void NotifyThemeChanged();
    }
}