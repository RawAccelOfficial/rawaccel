using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Globalization;
using userinterface.Services;
using userinterface.ViewModels;
using userinterface.ViewModels.Controls;
using Avalonia.Controls;
using System.Collections.Generic;

namespace userinterface.ViewModels.Settings;

public class SettingsPageViewModel : ViewModelBase
{
    public DualColumnLabelFieldViewModel SettingsFields { get; }

    public ObservableCollection<LanguageItem> AvailableLanguages { get; }

    private LanguageItem selectedLanguage;

    public SettingsPageViewModel()
    {
        SettingsFields = new DualColumnLabelFieldViewModel();
        SettingsFields.LabelWidth = 150;

        AvailableLanguages = new ObservableCollection<LanguageItem>
        {
            new LanguageItem("English", "en-US"),
            new LanguageItem("Spanish", "es-ES"),
            new LanguageItem("French", "fr-FR"),
            new LanguageItem("German", "de-DE"),
            new LanguageItem("Italian", "it-IT"),
            new LanguageItem("Portuguese", "pt-PT"),
            new LanguageItem("Russian", "ru-RU"),
            new LanguageItem("Chinese (Simplified)", "zh-CN"),
            new LanguageItem("Chinese (Traditional)", "zh-TW"),
            new LanguageItem("Japanese", "ja-JP"),
            new LanguageItem("Korean", "ko-KR"),
            new LanguageItem("Filipino", "fil-PH") // Added for testing
        };

        selectedLanguage = AvailableLanguages[0]; // Default to English

        InitializeSettingsFields();
    }

    private SettingsService SettingsService =>
        App.Services!.GetRequiredService<SettingsService>();

    private LocalizationService LocalizationService =>
        App.Services!.GetRequiredService<LocalizationService>();

    public bool ShowToastNotifications
    {
        get => SettingsService.ShowToastNotifications;
        set
        {
            SettingsService.ShowToastNotifications = value;
            OnPropertyChanged();
        }
    }

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
            LocalizationService.ChangeLanguage(cultureCode);
        }
        catch (CultureNotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"Culture not found: {cultureCode} - {ex.Message}");
        }
    }

    private void InitializeSettingsFields()
    {
        var languageComboBox = new ComboBox
        {
            ItemsSource = AvailableLanguages,
            SelectedItem = SelectedLanguage,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch,
            DisplayMemberBinding = new Avalonia.Data.Binding("DisplayName")
        };

        languageComboBox.SelectionChanged += (sender, e) =>
        {
            if (languageComboBox.SelectedItem is LanguageItem language)
            {
                SelectedLanguage = language;
            }
        };

        SettingsFields.AddField("Language", languageComboBox);

        var toastCheckBox = new CheckBox
        {
            IsChecked = ShowToastNotifications,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left
        };

        toastCheckBox.IsCheckedChanged += (sender, e) =>
        {
            ShowToastNotifications = toastCheckBox.IsChecked ?? false;
        };

        SettingsFields.AddField("Show Toast Notifications", toastCheckBox);
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