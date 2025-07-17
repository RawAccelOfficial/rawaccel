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

            // Set culture for all resource managers
            Properties.Resources.MainWindow.Culture = culture;
            // Properties.Resources.OtherFile.Culture = culture;

            // Notify that ALL properties have changed
            OnPropertyChanged(string.Empty);

            System.Diagnostics.Debug.WriteLine($"Language changed to: {culture.DisplayName} ({cultureCode})");
            System.Diagnostics.Debug.WriteLine($"Test resource value: {Properties.Resources.MainWindow.ApplySettingsButton}");
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