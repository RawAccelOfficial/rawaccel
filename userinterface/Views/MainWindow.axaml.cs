using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;
using userinterface.ViewModels;
using userinterface.Services;

namespace userinterface.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        UpdateThemeToggleButton();
        UpdateSelectedButton("Devices"); // Initial navigation selection
    }

    public void ApplyButtonHandler(object sender, RoutedEventArgs args)
    {
        if (this.DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ApplyButtonClicked();
        }
    }

    public void OnNavigationClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pageName)
        {
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.SelectPage(pageName);
                UpdateSelectedButton(pageName);
            }
        }
    }

    private void UpdateSelectedButton(string selectedPage)
    {
        DevicesButton.Classes.Remove("Selected");
        MappingsButton.Classes.Remove("Selected");
        ProfilesButton.Classes.Remove("Selected");

        switch (selectedPage)
        {
            case "Devices":
                DevicesButton.Classes.Add("Selected");
                break;
            case "Mappings":
                MappingsButton.Classes.Add("Selected");
                break;
            case "Profiles":
                ProfilesButton.Classes.Add("Selected");
                break;
        }
    }

    private void ToggleTheme(object sender, RoutedEventArgs e)
    {
        var currentTheme = Application.Current?.ActualThemeVariant;
        var newTheme = currentTheme == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = newTheme;
        }
        UpdateThemeToggleButton();
        ThemeService.NotifyThemeChanged();
    }


    private void UpdateThemeToggleButton()
    {
        var themeIcon = this.FindControl<PathIcon>("ThemeIcon");
        var toggleButton = this.FindControl<ToggleButton>("ThemeToggleButton");
        if (themeIcon != null && toggleButton != null)
        {
            var isDark = Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
            if (isDark)
            {
                themeIcon.Data = (Avalonia.Media.Geometry?)this.FindResource("weather_moon_regular");
            }
            else
            {
                themeIcon.Data = (Avalonia.Media.Geometry?)this.FindResource("weather_sunny_regular");
            }
            toggleButton.IsChecked = !isDark;
        }
    }
}
