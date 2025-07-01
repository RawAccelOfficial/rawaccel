using System;

namespace userinterface.Services
{
    public static class ThemeService
    {
        public static event EventHandler? ThemeChanged;

        public static void NotifyThemeChanged()
        {
            ThemeChanged?.Invoke(null, EventArgs.Empty);
        }
    }
}
