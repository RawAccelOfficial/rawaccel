using System.Collections.ObjectModel;
using System.Globalization;
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

    private SettingsService SettingsService =>
        App.Services!.GetRequiredService<SettingsService>();

    private LocalizationService LocalizationService =>
        App.Services!.GetRequiredService<LocalizationService>();

    public GeneralSettings GeneralSettings { get; }

    public NotificationSettings NotificationSettings { get; }

    public ICommand BugReportCommand { get; }

    private void OnGeneralSettingsChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(GeneralSettings.SelectedLanguage) && notificationService != null)
        {
            notificationService.ShowInfoToast($"Language changed to {GeneralSettings.SelectedLanguage.DisplayName}");
        }
    }
}

public class GeneralSettings : ViewModelBase
{
    private readonly SettingsService settingsService;
    private readonly LocalizationService localizationService;
    private LanguageItem selectedLanguage;

    public GeneralSettings(SettingsService settingsService, LocalizationService localizationService)
    {
        this.settingsService = settingsService;
        this.localizationService = localizationService;

        AvailableLanguages = new ObservableCollection<LanguageItem>
        {
            new LanguageItem("English", "en-US"),
            new LanguageItem("Japanese", "ja-JP"),
        };

        selectedLanguage = AvailableLanguages[0];
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
            localizationService.ChangeLanguage(cultureCode);
        }
        catch (CultureNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Culture not found: {cultureCode} - {ex.Message}");
        }
    }
}

public class NotificationSettings : ViewModelBase
{
    private readonly SettingsService settingsService;

    public NotificationSettings(SettingsService settingsService)
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