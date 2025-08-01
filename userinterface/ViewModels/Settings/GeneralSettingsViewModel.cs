using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using userinterface.Services;

namespace userinterface.ViewModels.Settings;

public class GeneralSettingsViewModel : ViewModelBase
{
    private readonly ISettingsService settingsService;
    private readonly LocalizationService localizationService;
    private readonly IThemeService themeService;
    private LanguageItem selectedLanguage;
    private string selectedThemeValue;

    public GeneralSettingsViewModel()
    {
        settingsService = App.Services!.GetRequiredService<ISettingsService>();
        localizationService = App.Services!.GetRequiredService<LocalizationService>();
        themeService = App.Services!.GetRequiredService<IThemeService>();

        AvailableLanguages = new ObservableCollection<LanguageItem>
        {
            new LanguageItem(localizationService.GetText("LanguageEN-US"), "en-US"),
            new LanguageItem(localizationService.GetText("LanguageJA-JP"), "ja-JP"),
        };

        ThemeLocalizationKeys = new List<string> { "ThemeSystem", "ThemeLight", "ThemeDark" };
        ThemeEnumValues = new List<string> { "System", "Light", "Dark" };

        // Set initial selected language based on current settings
        var currentLanguage = settingsService.Language;
        selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.CultureCode == currentLanguage) ?? AvailableLanguages[0];

        // Set initial selected theme based on current settings
        selectedThemeValue = settingsService.Theme;

        NotificationSettings = new NotificationSettings(settingsService);

        // Listen for theme changes from other sources (like the toggle button)
        settingsService.ThemeChanged += OnSettingsThemeChanged;

    }

    public ObservableCollection<LanguageItem> AvailableLanguages { get; }

    public IEnumerable<string> ThemeLocalizationKeys { get; }
    
    public IEnumerable<string> ThemeEnumValues { get; }

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

    public string SelectedThemeValue
    {
        get => selectedThemeValue;
        set
        {
            if (SetProperty(ref selectedThemeValue, value) && !string.IsNullOrEmpty(value))
            {
                ChangeTheme(value);
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

    private void ChangeTheme(string themeCode)
    {
        try
        {
            settingsService.Theme = themeCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to change theme: {themeCode} - {ex.Message}");
        }
    }

    private void OnSettingsThemeChanged(object? sender, EventArgs e)
    {
        // Update the selected theme value when the settings service notifies of changes
        var currentTheme = settingsService.Theme;
        if (selectedThemeValue != currentTheme)
        {
            selectedThemeValue = currentTheme;
            OnPropertyChanged(nameof(SelectedThemeValue));
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

    public bool ShowConfirmModals
    {
        get => settingsService.ShowConfirmModals;
        set
        {
            settingsService.ShowConfirmModals = value;
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