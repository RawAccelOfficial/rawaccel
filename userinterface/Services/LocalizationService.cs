using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace userinterface.Services;

public class LocalizationService : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    public void ChangeLanguage(string cultureCode)
    {
        try
        {
            var culture = new CultureInfo(cultureCode);
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;

            // Set culture for the consolidated resource manager
            Properties.Resources.Strings.Culture = culture;

            // Notify that ALL properties have changed
            OnPropertyChanged(string.Empty);
        }
        catch (CultureNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Culture not found: {cultureCode} - {ex.Message}");
        }
    }

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}