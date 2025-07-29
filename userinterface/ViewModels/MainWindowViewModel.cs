using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Linq;
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
using userinterface.Views;
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

        devicesPage = App.Services!.GetRequiredService<DevicesPageViewModel>();
        profilesPage = App.Services!.GetRequiredService<ProfilesPageViewModel>();
        mappingsPage = App.Services!.GetRequiredService<MappingsPageViewModel>();
        settingsPage = App.Services!.GetRequiredService<SettingsPageViewModel>();
        profileListView = App.Services!.GetRequiredService<ProfileListViewModel>();
        toastViewModel = App.Services!.GetRequiredService<ToastViewModel>();

        ApplyCommand = new RelayCommand(() => Apply());
        NavigateCommand = new RelayCommand<NavigationPage>(page => SelectPage(page));
        ToggleThemeCommand = new RelayCommand(() => ToggleTheme());
        
        profileListView.SelectedProfileChanged += OnProfileSelected;
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
                selectedPageValue = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentPageContent));
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
        SelectedPage = page;
        IsProfilesExpanded = page == NavigationPage.Profiles;
        
        if (page == NavigationPage.Profiles && profileListView.SelectedProfile == null)
        {
            var defaultProfile = backEnd.Profiles.Profiles.FirstOrDefault(p => p == BE.Model.ProfilesModel.DefaultProfile);
            if (defaultProfile != null)
            {
                profileListView.SelectedProfile = defaultProfile;
            }
            else if (backEnd.Profiles.Profiles.Count > 0)
            {
                profileListView.SelectedProfile = backEnd.Profiles.Profiles[0];
            }
        }
        
        UpdateNavigationButtonSelection(page);
    }
    
    private void UpdateNavigationButtonSelection(NavigationPage page)
    {
        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop && 
            desktop.MainWindow is MainWindow mainWindow)
        {
            mainWindow.UpdateNavigationSelection(page);
        }
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
        
        if (page == NavigationPage.Profiles && profileListView.SelectedProfile == null)
        {
            var defaultProfile = backEnd.Profiles.Profiles.FirstOrDefault(p => p == BE.Model.ProfilesModel.DefaultProfile);
            if (defaultProfile != null)
            {
                profileListView.SelectedProfile = defaultProfile;
            }
            else if (backEnd.Profiles.Profiles.Count > 0)
            {
                profileListView.SelectedProfile = backEnd.Profiles.Profiles[0];
            }
        }
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
            var actualSystemTheme = ThemeVariantConverter.GetSystemThemeVariant();
            newTheme = actualSystemTheme == ThemeVariant.Dark ? "Light" : "Dark";
        }
        else
        {
            newTheme = currentTheme == "light" ? "Dark" : "Light";
        }
        
        settingsService.Theme = newTheme;
    }
    
    private void OnProfileSelected(BE.Model.ProfileModel selectedProfile)
    {
        if (selectedProfile != null && SelectedPage != NavigationPage.Profiles)
        {
            SelectPage(NavigationPage.Profiles);
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual new void OnPropertyChanged([CallerMemberName] string? PropertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
    }
}