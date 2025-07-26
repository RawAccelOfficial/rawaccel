using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
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

        var toastCheckBox = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = generalSettingsViewModel.NotificationSettings
        };

        toastCheckBox.Bind(CheckBox.IsCheckedProperty, new Binding("ShowToastNotifications"));

        settingsFieldViewModel.AddField("SettingsShowToastNotifications", toastCheckBox);

        SettingsStackPanel.Children.Add(settingsField);
    }
}