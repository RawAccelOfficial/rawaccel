using System;

namespace userinterface.Services
{
    public class ThemeService : IThemeService
    {
        public event EventHandler? ThemeChanged;

        public void NotifyThemeChanged()
        {
            ThemeChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}