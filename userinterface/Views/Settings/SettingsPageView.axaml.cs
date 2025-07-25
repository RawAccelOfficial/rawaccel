using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Interactivity;
using Avalonia.Layout;
using userinterface.ViewModels.Controls;
using userinterface.ViewModels.Settings;
using userinterface.Views.Controls;

namespace userinterface.Views.Settings;

public partial class SettingsPageView : UserControl
{
    private DualColumnLabelFieldView? SettingsField;
    private DualColumnLabelFieldViewModel? SettingsFieldViewModel;

    public SettingsPageView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object? sender, RoutedEventArgs e)
    {
        if (SettingsField == null)
        {
            SetupControls();
        }
    }

    private void SetupControls()
    {
        if (DataContext is not SettingsPageViewModel viewModel)
        {
            return;
        }

        CreateSettingsFieldViewModel();
        AddSettingsFields(viewModel);
        AddControlToStackPanel();
    }

    private void CreateSettingsFieldViewModel()
    {
        SettingsFieldViewModel = new DualColumnLabelFieldViewModel();
        SettingsField = new DualColumnLabelFieldView(SettingsFieldViewModel);
    }

    private void AddSettingsFields(SettingsPageViewModel viewModel)
    {
        if (SettingsFieldViewModel == null)
            return;

        var languageComboBox = new ComboBox
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = viewModel.GeneralSettings
        };

        languageComboBox.Bind(ComboBox.ItemsSourceProperty, new Binding("AvailableLanguages"));
        languageComboBox.Bind(ComboBox.SelectedItemProperty, new Binding("SelectedLanguage"));
        languageComboBox.DisplayMemberBinding = new Binding("DisplayName");

        SettingsFieldViewModel.AddField("SettingsLanguage", languageComboBox);

        var toastCheckBox = new CheckBox
        {
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            DataContext = viewModel.NotificationSettings
        };

        toastCheckBox.Bind(CheckBox.IsCheckedProperty, new Binding("ShowToastNotifications"));

        SettingsFieldViewModel.AddField("SettingsShowToastNotifications", toastCheckBox);
    }

    private void AddControlToStackPanel()
    {
        if (SettingsField == null)
        {
            return;
        }

        var settingsStackPanel = this.FindControl<StackPanel>("SettingsStackPanel");
        settingsStackPanel?.Children.Add(SettingsField);
    }
}