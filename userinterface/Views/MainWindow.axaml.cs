using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using userinterface.Models;
using userinterface.Services;
using userinterface.ViewModels;

namespace userinterface.Views;

public partial class MainWindow : Window
{
    private Button? ApplyButtonControl;
    private ProgressBar? LoadingProgressBar;

    public MainWindow()
    {
        InitializeComponent();

        InitializeControls();
        UpdateThemeToggleButton();
        UpdateSelectedButton(NavigationPage.Devices);
    }

    private INotificationService NotificationService =>
        App.Services!.GetRequiredService<INotificationService>();

    private void InitializeControls()
    {
        ApplyButtonControl = this.FindControl<Button>("ApplyButton");
        LoadingProgressBar = this.FindControl<ProgressBar>("LoadingProgress");

        // Subscribe to click events
        if (ApplyButtonControl != null)
        {
            ApplyButtonControl.Click += ApplyButtonHandler;
        }

        var settingsButton = this.FindControl<Button>("SettingsButton");
        if (settingsButton != null)
        {
            settingsButton.Click += OnSettingsClick;
        }

        var themeToggleButton = this.FindControl<ToggleButton>("ThemeToggleButton");
        if (themeToggleButton != null)
        {
            themeToggleButton.Click += ToggleTheme;
        }

        var devicesButton = this.FindControl<Button>("DevicesButton");
        if (devicesButton != null)
        {
            devicesButton.Click += OnNavigationClick;
        }

        var mappingsButton = this.FindControl<Button>("MappingsButton");
        if (mappingsButton != null)
        {
            mappingsButton.Click += OnNavigationClick;
        }

        var profilesButton = this.FindControl<Button>("ProfilesButton");
        if (profilesButton != null)
        {
            profilesButton.Click += OnNavigationClick;
        }
    }

    public async void ApplyButtonHandler(object sender, RoutedEventArgs args)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (ApplyButtonControl != null)
            {
                ApplyButtonControl.IsEnabled = false;
            }
            if (LoadingProgressBar != null)
            {
                LoadingProgressBar.IsVisible = true;
            }

            if (viewModel.ApplyCommand.CanExecute(null))
            {
                viewModel.ApplyCommand.Execute(null);
            }

            await Task.Delay(1000);

            if (LoadingProgressBar != null)
            {
                LoadingProgressBar.IsVisible = false;
            }

            NotificationService.ShowSuccessToast("Settings applied successfully!");

            if (ApplyButtonControl != null)
            {
                ApplyButtonControl.IsEnabled = true;
            }
        }
    }

    public void OnNavigationClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string pageNameString)
        {
            if (Enum.TryParse<NavigationPage>(pageNameString, out var page))
            {
                if (DataContext is MainWindowViewModel viewModel)
                {
                    if (viewModel.NavigateCommand.CanExecute(page))
                    {
                        viewModel.NavigateCommand.Execute(page);
                    }
                    UpdateSelectedButton(page);
                }
            }
        }
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (viewModel.NavigateCommand.CanExecute(NavigationPage.Settings))
            {
                viewModel.NavigateCommand.Execute(NavigationPage.Settings);
            }
            UpdateSelectedButton(NavigationPage.Settings);
        }
    }

    private void ToggleTheme(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            if (viewModel.ToggleThemeCommand.CanExecute(null))
            {
                viewModel.ToggleThemeCommand.Execute(null);
            }
        }
        UpdateThemeToggleButton();
    }

    private void UpdateSelectedButton(NavigationPage selectedPage)
    {
        var devicesButton = this.FindControl<Button>("DevicesButton");
        var mappingsButton = this.FindControl<Button>("MappingsButton");
        var profilesButton = this.FindControl<Button>("ProfilesButton");
        var settingsButton = this.FindControl<Button>("SettingsButton");

        devicesButton?.Classes.Remove("Selected");
        mappingsButton?.Classes.Remove("Selected");
        profilesButton?.Classes.Remove("Selected");
        settingsButton?.Classes.Remove("Selected");

        switch (selectedPage)
        {
            case NavigationPage.Devices:
                devicesButton?.Classes.Add("Selected");
                break;

            case NavigationPage.Mappings:
                mappingsButton?.Classes.Add("Selected");
                break;

            case NavigationPage.Profiles:
                profilesButton?.Classes.Add("Selected");
                break;

            case NavigationPage.Settings:
                settingsButton?.Classes.Add("Selected");
                break;
        }
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