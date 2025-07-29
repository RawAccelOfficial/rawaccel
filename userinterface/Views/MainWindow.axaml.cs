using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading.Tasks;
using userinterface.Converters;
using userinterface.Extensions;
using userinterface.Models;
using userinterface.Services;
using userinterface.ViewModels;
using userinterface.Views.Controls;

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
        
        // Subscribe to theme changes
        ThemeService.ThemeChanged += OnThemeChanged;
    }

    private INotificationService NotificationService =>
        App.Services!.GetRequiredService<INotificationService>();
    
    private ISettingsService SettingsService =>
        App.Services!.GetRequiredService<ISettingsService>();
    
    private IThemeService ThemeService =>
        App.Services!.GetRequiredService<IThemeService>();

    private void InitializeControls()
    {
        ApplyButtonControl = this.FindControl<Button>("ApplyButton");
        LoadingProgressBar = this.FindControl<ProgressBar>("LoadingProgress");

        if (ApplyButtonControl != null)
        {
            ApplyButtonControl.Click += ApplyButtonHandler;
        }

        if (this.TryFindControl<Button>("SettingsButton", out var settingsButton))
        {
            settingsButton.Click += OnSettingsClick;
        }

        if (this.TryFindControl<ToggleButton>("ThemeToggleButton", out var themeToggleButton))
        {
            themeToggleButton.Click += ToggleTheme;
        }

        if (this.TryFindControl<Button>("DevicesButton", out var devicesButton))
        {
            devicesButton.Click += OnNavigationClick;
        }

        if (this.TryFindControl<Button>("MappingsButton", out var mappingsButton))
        {
            mappingsButton.Click += OnNavigationClick;
        }

        if (this.TryFindControl<Button>("ProfilesButton", out var profilesButton))
        {
            profilesButton.Click += OnNavigationClick;
        }
    }

    public void ApplyButtonHandler(object? sender, RoutedEventArgs args)
    {
        // TODO: Revert this to the original apply logic after fixing the graph initialization issue
        if (DataContext is MainWindowViewModel viewModel)
        {
            // Test animation instead of applying settings
            viewModel.IsProfilesExpanded = !viewModel.IsProfilesExpanded;

            /* Original apply logic commented out for animation testing
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

            NotificationService.ShowSuccessToast("MainWindowSettingsAppliedSuccess");

            if (ApplyButtonControl != null)
            {
                ApplyButtonControl.IsEnabled = true;
            }
            */
        }
    }

    public async void OnNavigationClick(object? sender, RoutedEventArgs e)
    {
        if (sender is Button button &&
            button.Tag is string pageNameString &&
            Enum.TryParse<NavigationPage>(pageNameString, out var page) &&
            DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.SelectPageAsync(page);
            UpdateSelectedButton(page);
        }
    }

    private async void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.SelectPageAsync(NavigationPage.Settings);
            UpdateSelectedButton(NavigationPage.Settings);
        }
    }

    private void ToggleTheme(object? sender, RoutedEventArgs e)
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
        this.TryFindControl<Button>("DevicesButton", out var devicesButton);
        this.TryFindControl<Button>("MappingsButton", out var mappingsButton);
        this.TryFindControl<Button>("ProfilesButton", out var profilesButton);
        this.TryFindControl<Button>("SettingsButton", out var settingsButton);

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
    
    public void UpdateNavigationSelection(NavigationPage page)
    {
        UpdateSelectedButton(page);
    }


    private void UpdateThemeToggleButton()
    {
        if (this.TryFindControl<PathIcon>("ThemeIcon", out var themeIcon) &&
            this.TryFindControl<ToggleButton>("ThemeToggleButton", out var toggleButton))
        {
            var currentTheme = SettingsService.Theme;
            var actualTheme = ThemeVariantConverter.GetActualTheme(currentTheme);
            
            if (actualTheme == ThemeVariant.Dark)
            {
                themeIcon.Data = (Avalonia.Media.Geometry?)this.FindResource("weather_moon_regular");
                toggleButton.IsChecked = true;
            }
            else
            {
                themeIcon.Data = (Avalonia.Media.Geometry?)this.FindResource("weather_sunny_regular");
                toggleButton.IsChecked = false;
            }
        }
    }


    private void OnThemeChanged(object? sender, EventArgs e)
    {
        UpdateThemeToggleButton();
    }
}