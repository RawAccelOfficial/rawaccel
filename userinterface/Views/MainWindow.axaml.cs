using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using System.Threading.Tasks;
using userinterface.ViewModels;
using userinterface.Services;

namespace userinterface.Views;

public partial class MainWindow : Window
{
    private Button? ApplyButtonControl;
    private ProgressBar? LoadingProgressBar;
    private TextBlock? SuccessMessageText;

    public MainWindow()
    {
        InitializeComponent();
        UpdateThemeToggleButton();
        UpdateSelectedButton("Devices"); // Initial navigation selection
        ApplyButtonControl = this.FindControl<Button>("ApplyButton");
        LoadingProgressBar = this.FindControl<ProgressBar>("LoadingProgress");
        SuccessMessageText = this.FindControl<TextBlock>("SuccessMessage");
    }

    public async void ApplyButtonHandler(object sender, RoutedEventArgs args)
    {
        // Disable the button and show loading
        if (ApplyButtonControl != null)
        {
            ApplyButtonControl.IsEnabled = false;
        }
        if (LoadingProgressBar != null)
        {
            LoadingProgressBar.IsVisible = true;
        }
        // Hide success message if it was previously shown
        if (SuccessMessageText != null)
        {
            SuccessMessageText.IsVisible = false;
            SuccessMessageText.Opacity = 0;
        }

        if (this.DataContext is MainWindowViewModel viewModel)
        {
            viewModel.Apply();
        }

        // Wait for 1 second to mask write delay
        await Task.Delay(1000);

        // Hide loading bar
        if (LoadingProgressBar != null)
        {
            LoadingProgressBar.IsVisible = false;
        }

        if (SuccessMessageText != null)
        {
            SuccessMessageText.IsVisible = true;

            await Dispatcher.UIThread.InvokeAsync(async () =>
            {
                SuccessMessageText.Opacity = 1;
                ApplyButtonControl.IsEnabled = true;
                // Hide the success message after 1.5 seconds
                await Task.Delay(1500);
                SuccessMessageText.Opacity = 0;
                await Task.Delay(300);
                SuccessMessageText.IsVisible = false;

            });
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
