using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using System.Threading;

namespace userinterface.Services
{
    public class LocalizationService : ILocalizationService
    {
        private readonly ResourceManager resourceManager;
        private CultureInfo currentCulture;

        public LocalizationService()
        {
            resourceManager = new ResourceManager(typeof(Strings));
            currentCulture = CultureInfo.CurrentUICulture;
        }

        public CultureInfo CurrentCulture => currentCulture;

        public IEnumerable<CultureInfo> AvailableCultures => new[]
        {
            new CultureInfo("en"),
            new CultureInfo("es"),
            new CultureInfo("fr"),
            new CultureInfo("de")
        };

        public event EventHandler<CultureInfo>? CultureChanged;

        public string GetString(string key)
        {
            try
            {
                return resourceManager.GetString(key, currentCulture) ?? key;
            }
            catch
            {
                return key;
            }
        }

        public void SetCulture(CultureInfo culture)
        {
            if (culture == null) return;

            currentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            CultureInfo.CurrentCulture = culture;

            CultureChanged?.Invoke(this, culture);
        }
    }
}