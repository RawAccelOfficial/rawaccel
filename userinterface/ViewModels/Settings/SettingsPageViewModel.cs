using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using userinterface.Services;
using userinterface.ViewModels;
using userinterface.ViewModels.Controls;
using Avalonia.Controls;

namespace userinterface.ViewModels.Settings;

public class SettingsPageViewModel : ViewModelBase
{
    public DualColumnLabelFieldViewModel SettingsFields { get; }

    public ObservableCollection<string> AvailableLanguages { get; }

    private string selectedLanguage = "English";

    public SettingsPageViewModel()
    {
        SettingsFields = new DualColumnLabelFieldViewModel();
        SettingsFields.LabelWidth = 150;

        // Initialize available languages
        AvailableLanguages = new ObservableCollection<string>
        {
            "English",
            "Spanish",
            "French",
            "German",
            "Italian",
            "Portuguese",
            "Russian",
            "Chinese (Simplified)",
            "Chinese (Traditional)",
            "Japanese",
            "Korean"
        };

        InitializeSettingsFields();
    }

    private SettingsService SettingsService =>
        App.Services!.GetRequiredService<SettingsService>();

    public bool ShowToastNotifications
    {
        get => SettingsService.ShowToastNotifications;
        set
        {
            SettingsService.ShowToastNotifications = value;
            OnPropertyChanged();
        }
    }

    public string SelectedLanguage
    {
        get => selectedLanguage;
        set
        {
            if (SetProperty(ref selectedLanguage, value))
            {
                // Here you would typically save the language preference
                // SettingsService.Language = value;
            }
        }
    }

    private void InitializeSettingsFields()
    {
        // Add language selection dropdown
        var languageComboBox = new ComboBox
        {
            ItemsSource = AvailableLanguages,
            SelectedItem = SelectedLanguage,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch
        };

        languageComboBox.SelectionChanged += (sender, e) =>
        {
            if (languageComboBox.SelectedItem is string language)
            {
                SelectedLanguage = language;
            }
        };

        SettingsFields.AddField("Language", languageComboBox);

        // Add toast notifications checkbox
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

        // You can add more settings here as needed
        // Example: Theme selection, auto-save interval, etc.
    }
}
