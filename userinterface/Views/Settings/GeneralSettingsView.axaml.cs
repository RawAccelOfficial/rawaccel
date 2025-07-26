using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using System.Linq;
using userinterface.Controls;
using userinterface.ViewModels.Controls;
using userinterface.ViewModels.Settings;
using userinterface.Views.Controls;

namespace userinterface.Views.Settings;

public partial class GeneralSettingsView : UserControl
{
    public GeneralSettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is GeneralSettingsViewModel generalSettingsViewModel)
        {
            SetupSettings(generalSettingsViewModel);
        }
    }

    private void SetupSettings(GeneralSettingsViewModel generalSettingsViewModel)
    {
        SettingsStackPanel.Children.Clear();

        var settingsFieldViewModel = new DualColumnLabelFieldViewModel();
        var settingsField = new DualColumnLabelFieldView(settingsFieldViewModel);

        var languageComboBox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = generalSettingsViewModel
        };

        languageComboBox.Bind(ComboBox.ItemsSourceProperty, new Binding("AvailableLanguages"));
        languageComboBox.Bind(ComboBox.SelectedItemProperty, new Binding("SelectedLanguage"));
        languageComboBox.DisplayMemberBinding = new Binding("DisplayName");

        settingsFieldViewModel.AddField("SettingsLanguage", languageComboBox);

        var themeComboBox = new LocalizedComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = generalSettingsViewModel
        };

        themeComboBox.Bind(LocalizedComboBox.LocalizationKeysProperty, new Binding("ThemeLocalizationKeys"));
        themeComboBox.Bind(LocalizedComboBox.EnumValuesProperty, new Binding("ThemeEnumValues"));
        
        // Handle selection changes
        themeComboBox.SelectionChanged += (sender, e) =>
        {
            if (themeComboBox.SelectedEnumValue != null)
            {
                generalSettingsViewModel.SelectedThemeValue = themeComboBox.SelectedEnumValue;
            }
        };

        // Set initial selection after the LocalizedComboBox is loaded
        themeComboBox.Loaded += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(generalSettingsViewModel.SelectedThemeValue))
            {
                var targetItem = themeComboBox.localizedItems.FirstOrDefault(item => item.EnumValue == generalSettingsViewModel.SelectedThemeValue);
                if (targetItem != null)
                {
                    themeComboBox.SelectedItem = targetItem;
                }
            }
        };

        settingsFieldViewModel.AddField("SettingsTheme", themeComboBox);

        var toastCheckBox = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = generalSettingsViewModel.NotificationSettings
        };

        toastCheckBox.Bind(CheckBox.IsCheckedProperty, new Binding("ShowToastNotifications"));

        settingsFieldViewModel.AddField("SettingsShowToastNotifications", toastCheckBox);

        var confirmModalsCheckBox = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = generalSettingsViewModel.NotificationSettings
        };

        confirmModalsCheckBox.Bind(CheckBox.IsCheckedProperty, new Binding("ShowConfirmModals"));

        settingsFieldViewModel.AddField("SettingsShowConfirmModals", confirmModalsCheckBox);

        SettingsStackPanel.Children.Add(settingsField);
    }
}