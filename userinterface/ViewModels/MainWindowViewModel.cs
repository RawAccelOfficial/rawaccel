using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using userinterface.Commands;
using userinterface.Converters;
using userinterface.Interfaces;
using userinterface.Models;
using userinterface.Services;
using userinterface.ViewModels.Controls;
using userinterface.ViewModels.Device;
using userinterface.ViewModels.Mapping;
using userinterface.ViewModels.Profile;
using userinterface.ViewModels.Settings;
using BE = userspace_backend;

namespace userinterface.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    private NavigationPage selectedPageValue = NavigationPage.Devices;
    private bool isProfilesExpandedValue = false;

    // Pre-created ViewModels
    private readonly DevicesPageViewModel devicesPage;
    private readonly ProfilesPageViewModel profilesPage;
    private readonly MappingsPageViewModel mappingsPage;
    private readonly SettingsPageViewModel settingsPage;
    private readonly ProfileListViewModel profileListView;
    private readonly ToastViewModel toastViewModel;

    private readonly BE.BackEnd backEnd;
    private readonly IThemeService themeService;
    private readonly ISettingsService settingsService;
    private readonly FrameTimerService frameTimer;

    public MainWindowViewModel(BE.BackEnd backEnd, IThemeService themeService, ISettingsService settingsService, FrameTimerService frameTimer)
    {
        this.backEnd = backEnd ?? throw new ArgumentNullException(nameof(backEnd));
        this.themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));
        this.frameTimer = frameTimer ?? throw new ArgumentNullException(nameof(frameTimer));

        // Pre-create all ViewModels to avoid lazy loading delays
        var stopwatch = Stopwatch.StartNew();
        
        devicesPage = App.Services!.GetRequiredService<DevicesPageViewModel>();
        Debug.WriteLine($"DevicesPageViewModel creation: {stopwatch.ElapsedMilliseconds}ms");
        
        stopwatch.Restart();
        profilesPage = App.Services!.GetRequiredService<ProfilesPageViewModel>();
        Debug.WriteLine($"ProfilesPageViewModel creation: {stopwatch.ElapsedMilliseconds}ms");
        
        stopwatch.Restart();
        mappingsPage = App.Services!.GetRequiredService<MappingsPageViewModel>();
        Debug.WriteLine($"MappingsPageViewModel creation: {stopwatch.ElapsedMilliseconds}ms");
        
        stopwatch.Restart();
        settingsPage = App.Services!.GetRequiredService<SettingsPageViewModel>();
        Debug.WriteLine($"SettingsPageViewModel creation: {stopwatch.ElapsedMilliseconds}ms");
        
        stopwatch.Restart();
        profileListView = App.Services!.GetRequiredService<ProfileListViewModel>();
        Debug.WriteLine($"ProfileListViewModel creation: {stopwatch.ElapsedMilliseconds}ms");
        
        stopwatch.Restart();
        toastViewModel = App.Services!.GetRequiredService<ToastViewModel>();
        Debug.WriteLine($"ToastViewModel creation: {stopwatch.ElapsedMilliseconds}ms");

        ApplyCommand = new RelayCommand(() => Apply());
        NavigateCommand = new RelayCommand<NavigationPage>(page => SelectPage(page));
        ToggleThemeCommand = new RelayCommand(() => ToggleTheme());
    }

    public DevicesPageViewModel DevicesPage => devicesPage;

    public ProfilesPageViewModel ProfilesPage => profilesPage;

    public MappingsPageViewModel MappingsPage => mappingsPage;

    public SettingsPageViewModel SettingsPage => settingsPage;

    public ProfileListViewModel ProfileListView => profileListView;

    public ToastViewModel ToastViewModel => toastViewModel;

    protected BE.BackEnd BackEnd => backEnd;

    public ICommand ApplyCommand { get; }

    public ICommand NavigateCommand { get; }

    public ICommand ToggleThemeCommand { get; }

    public NavigationPage SelectedPage
    {
        get => selectedPageValue;
        set
        {
            if (selectedPageValue != value)
            {
                var stopwatch = Stopwatch.StartNew();
                selectedPageValue = value;
                Debug.WriteLine($"SelectedPage value set: {stopwatch.ElapsedMilliseconds}ms");
                
                stopwatch.Restart();
                OnPropertyChanged();
                Debug.WriteLine($"SelectedPage PropertyChanged: {stopwatch.ElapsedMilliseconds}ms");
                
                stopwatch.Restart();
                OnPropertyChanged(nameof(CurrentPageContent));
                Debug.WriteLine($"CurrentPageContent PropertyChanged: {stopwatch.ElapsedMilliseconds}ms");
            }
        }
    }

    public bool IsProfilesExpanded
    {
        get => isProfilesExpandedValue;
        set
        {
            if (isProfilesExpandedValue != value)
            {
                isProfilesExpandedValue = value;
                OnPropertyChanged();

                if (value)
                {
                    ExpandProfiles();
                }
                else
                {
                    CollapseProfiles();
                }
            }
        }
    }

    public object? CurrentPageContent =>
        SelectedPage switch
        {
            NavigationPage.Devices => DevicesPage,
            NavigationPage.Mappings => MappingsPage,
            NavigationPage.Profiles => ProfilesPage,
            NavigationPage.Settings => SettingsPage,
            _ => DevicesPage
        };

    public void SelectPage(NavigationPage page)
    {
        var stopwatch = Stopwatch.StartNew();
        Debug.WriteLine($"SelectPage called for {page}");
        
        // Start frame timing to detect UI thread blocking
        frameTimer.StartMonitoring($"Page switch to {page}");
        frameTimer.StartRenderMonitoring($"Page switch to {page} rendering");
        
        SelectedPage = page;
        Debug.WriteLine($"SelectedPage set: {stopwatch.ElapsedMilliseconds}ms");
        
        stopwatch.Restart();
        IsProfilesExpanded = page == NavigationPage.Profiles;
        Debug.WriteLine($"IsProfilesExpanded set: {stopwatch.ElapsedMilliseconds}ms");
        
        Debug.WriteLine($"Total SelectPage time: {stopwatch.ElapsedMilliseconds}ms");
        
        // Stop frame timing after a longer delay to capture the full animation cycle
        _ = Task.Delay(500).ContinueWith(_ => 
        {
            frameTimer.StopRenderMonitoring($"Page switch to {page} rendering completed");
            frameTimer.StopMonitoring($"Page switch to {page} completed");
        });
    }

    public async Task SelectPageAsync(NavigationPage page)
    {
        ViewModelBase pageViewModel = page switch
        {
            NavigationPage.Devices => DevicesPage,
            NavigationPage.Profiles => ProfilesPage,
            NavigationPage.Mappings => MappingsPage,
            NavigationPage.Settings => SettingsPage,
            _ => DevicesPage
        };

        if (pageViewModel is IAsyncInitializable asyncViewModel && !asyncViewModel.IsInitialized)
        {
            await asyncViewModel.InitializeAsync();
        }

        SelectedPage = page;
        IsProfilesExpanded = page == NavigationPage.Profiles;
    }

    private async void ExpandProfiles()
    {
        var view = App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.FindControl<userinterface.Views.Profile.ProfileListView>("ProfileListView")
            : null;

        if (view != null)
        {
            await view.ExpandElements();
        }
    }

    private async void CollapseProfiles()
    {
        var view = App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.FindControl<userinterface.Views.Profile.ProfileListView>("ProfileListView")
            : null;

        if (view != null)
        {
            await view.CollapseElements();
        }
    }

    public void Apply()
    {
        BackEnd.Apply();
    }

    private void ToggleTheme()
    {
        var currentTheme = settingsService.Theme.ToLower();
        string newTheme;
        
        if (currentTheme == "system")
        {
            // If we're on system theme, check what the actual system theme is
            var actualSystemTheme = ThemeVariantConverter.GetSystemThemeVariant();
            // Toggle to the opposite of what the system currently is
            newTheme = actualSystemTheme == ThemeVariant.Dark ? "Light" : "Dark";
        }
        else
        {
            // Normal toggle between Light and Dark
            newTheme = currentTheme == "light" ? "Dark" : "Light";
        }
        
        settingsService.Theme = newTheme;
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual new void OnPropertyChanged([CallerMemberName] string? PropertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
    }
}