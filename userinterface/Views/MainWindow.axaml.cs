using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;
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
    private readonly INotificationService notificationService;
    private readonly IModalService modalService;

    public MainWindow(INotificationService notificationService, IModalService modalService)
    {
        InitializeComponent();
        this.notificationService = notificationService;
        this.modalService = modalService;
        UpdateThemeToggleButton();
        UpdateSelectedButton(NavigationPage.Devices);
        ApplyButtonControl = this.FindControl<Button>("ApplyButton");
        LoadingProgressBar = this.FindControl<ProgressBar>("LoadingProgress");

        // Subscribe to settings button click
        var settingsButton = this.FindControl<Button>("SettingsButton");
        if (settingsButton != null)
        {
            settingsButton.Click += OnSettingsClick;
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

            notificationService.ShowSuccessToast("Settings applied successfully!");

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

    private void UpdateSelectedButton(NavigationPage selectedPage)
    {
        DevicesButton.Classes.Remove("Selected");
        MappingsButton.Classes.Remove("Selected");
        ProfilesButton.Classes.Remove("Selected");

        var settingsButton = this.FindControl<Button>("SettingsButton");
        settingsButton?.Classes.Remove("Selected");

        switch (selectedPage)
        {
            case NavigationPage.Devices:
                DevicesButton.Classes.Add("Selected");
                break;

            case NavigationPage.Mappings:
                MappingsButton.Classes.Add("Selected");
                break;

            case NavigationPage.Profiles:
                ProfilesButton.Classes.Add("Selected");
                break;

            case NavigationPage.Settings:
                settingsButton?.Classes.Add("Selected");
                break;
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