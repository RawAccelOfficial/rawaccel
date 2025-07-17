using System;
using System.Collections.Generic;
using System.Globalization;

namespace userinterface.Services
{
    public interface ILocalizationService
    {
        string GetString(string key);
        void SetCulture(CultureInfo culture);
        CultureInfo CurrentCulture { get; }
        IEnumerable<CultureInfo> AvailableCultures { get; }
        event EventHandler<CultureInfo> CultureChanged;
    }
}