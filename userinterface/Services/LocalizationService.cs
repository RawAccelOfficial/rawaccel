using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace userinterface.Services;

public class LocalizationService : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public bool TryChangeLanguage(string cultureCode, out CultureInfo? culture)
    {
        culture = null;
        try
        {
            culture = new CultureInfo(cultureCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            // Set culture for the consolidated resource manager
            Properties.Resources.Strings.Culture = culture;

            // Notify that ALL properties have changed
            OnPropertyChanged(string.Empty);
            return true;
        }
        catch (CultureNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Culture not found: {cultureCode} - {ex.Message}");
            return false;
        }
    }

    public void ChangeLanguage(string cultureCode)
    {
        TryChangeLanguage(cultureCode, out _);
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}