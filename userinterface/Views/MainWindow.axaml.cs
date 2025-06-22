using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;
using userinterface.ViewModels;

namespace userinterface.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        UpdateThemeToggleButton();
    }

    public void ApplyButtonHandler(object sender, RoutedEventArgs args)
    {
        if (this.DataContext is MainWindowViewModel viewModel)
        {
            viewModel.ApplyButtonClicked();
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
