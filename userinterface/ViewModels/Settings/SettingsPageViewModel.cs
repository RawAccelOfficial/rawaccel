using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;
using userinterface.Commands;
using userinterface.Services;
using userinterface.ViewModels;
using userinterface.ViewModels.Controls;

namespace userinterface.ViewModels.Settings;

public class SettingsPageViewModel : ViewModelBase
{
    private readonly INotificationService? notificationService;

    public SettingsPageViewModel()
    {
        notificationService = App.Services?.GetService<INotificationService>();

        GeneralSettings = new GeneralSettings(SettingsService, LocalizationService);
        NotificationSettings = new NotificationSettings(SettingsService);

        GeneralSettings.PropertyChanged += OnGeneralSettingsChanged;

        BugReportCommand = new RelayCommand(() => App.OpenBugReportUrl());
    }

    private ISettingsService SettingsService =>
        App.Services!.GetRequiredService<ISettingsService>();

    private LocalizationService LocalizationService =>
        App.Services!.GetRequiredService<LocalizationService>();

    public GeneralSettings GeneralSettings { get; }

    public NotificationSettings NotificationSettings { get; }

    public ICommand BugReportCommand { get; }

    private void OnGeneralSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Language change notification is now handled in ChangeLanguage method
    }
}

public class GeneralSettings : ViewModelBase
{
    private readonly ISettingsService settingsService;
    private readonly LocalizationService localizationService;
    private LanguageItem selectedLanguage;

    public GeneralSettings(ISettingsService settingsService, LocalizationService localizationService)
    {
        this.settingsService = settingsService;
        this.localizationService = localizationService;

        AvailableLanguages = new ObservableCollection<LanguageItem>
        {
            new LanguageItem(localizationService.GetText("LanguageEN-US"), "en-US"),
            new LanguageItem(localizationService.GetText("LanguageJA-JP"), "ja-JP"),
        };

        // Set initial selected language based on current settings
        var currentLanguage = settingsService.Language;
        selectedLanguage = AvailableLanguages.FirstOrDefault(l => l.CultureCode == currentLanguage) ?? AvailableLanguages[0];
    }

    public ObservableCollection<LanguageItem> AvailableLanguages { get; }

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