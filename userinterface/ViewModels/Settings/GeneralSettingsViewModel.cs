using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using userinterface.Services;

namespace userinterface.ViewModels.Settings;

public class GeneralSettingsViewModel : ViewModelBase
{
    private readonly ISettingsService settingsService;
    private readonly LocalizationService localizationService;
    private LanguageItem selectedLanguage;

    public GeneralSettingsViewModel()
    {
        settingsService = App.Services!.GetRequiredService<ISettingsService>();
        localizationService = App.Services!.GetRequiredService<LocalizationService>();

        AvailableLanguages = new ObservableCollection<LanguageItem>
        {
            new LanguageItem(localizationService.GetText("LanguageEN-US"), "en-US"),
            new LanguageItem(localizationService.GetText("LanguageJA-JP"), "ja-JP"),
        };

        // Set initial selected language based on current settings
        var currentLanguage = settingsService.Language;
        selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.CultureCode == currentLanguage) ?? AvailableLanguages[0];

        NotificationSettings = new NotificationSettings(settingsService);
    }

    public ObservableCollection<LanguageItem> AvailableLanguages { get; }

    public NotificationSettings NotificationSettings { get; }

    public LanguageItem SelectedLanguage
    {
        get => selectedLanguage;
        set
        {
            if (SetProperty(ref selectedLanguage, value))
            {
                ChangeLanguage(value.CultureCode);
            }
        }
    }

    private void ChangeLanguage(string cultureCode)
    {
        try
        {
            // Get the native language name before changing the language
            var languageName = selectedLanguage.DisplayName;

            // Change the language first
            localizationService.ChangeLanguage(cultureCode);
            settingsService.Language = cultureCode;

            // Now show the notification in the new language
            var notificationService = App.Services?.GetService<INotificationService>();
            if (notificationService != null)
            {
                notificationService.ShowInfoToast("SettingsLanguageChangedTo", 4000, languageName);
            }
        }
        catch (CultureNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Culture not found: {cultureCode} - {ex.Message}");
        }
    }
}

public class NotificationSettings : ViewModelBase
{
    private readonly ISettingsService settingsService;

    public NotificationSettings(ISettingsService settingsService)
    {
        this.settingsService = settingsService;
    }

    public bool ShowToastNotifications
    {
        get => settingsService.ShowToastNotifications;
        set
        {
            settingsService.ShowToastNotifications = value;
            OnPropertyChanged();
        }
    }
}

public class LanguageItem
{
    public string DisplayName { get; }
    public string CultureCode { get; }

    public LanguageItem(string displayName, string cultureCode)
    {
        DisplayName = displayName;
        CultureCode = cultureCode;
    }

    public override string ToString() => DisplayName;
}