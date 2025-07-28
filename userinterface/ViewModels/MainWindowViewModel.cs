using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
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

    // Lazy-loaded ViewModels
    private DevicesPageViewModel? devicesPage;
    private ProfilesPageViewModel? profilesPage;
    private MappingsPageViewModel? mappingsPage;
    private SettingsPageViewModel? settingsPage;
    private ProfileListViewModel? profileListView;
    private ToastViewModel? toastViewModel;

    private readonly BE.BackEnd backEnd;
    private readonly IThemeService themeService;
    private readonly ISettingsService settingsService;

    public MainWindowViewModel(BE.BackEnd backEnd, IThemeService themeService, ISettingsService settingsService)
    {
        this.backEnd = backEnd ?? throw new ArgumentNullException(nameof(backEnd));
        this.themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));
        this.settingsService = settingsService ?? throw new ArgumentNullException(nameof(settingsService));

        ApplyCommand = new RelayCommand(() => Apply());
        NavigateCommand = new RelayCommand<NavigationPage>(page => SelectPage(page));
        ToggleThemeCommand = new RelayCommand(() => ToggleTheme());
    }

    public DevicesPageViewModel DevicesPage =>
        devicesPage ??= App.Services!.GetRequiredService<DevicesPageViewModel>();

    public ProfilesPageViewModel ProfilesPage =>
        profilesPage ??= App.Services!.GetRequiredService<ProfilesPageViewModel>();

    public MappingsPageViewModel MappingsPage =>
        mappingsPage ??= App.Services!.GetRequiredService<MappingsPageViewModel>();

    public SettingsPageViewModel SettingsPage =>
        settingsPage ??= App.Services!.GetRequiredService<SettingsPageViewModel>();

    public ProfileListViewModel ProfileListView =>
        profileListView ??= App.Services!.GetRequiredService<ProfileListViewModel>();

    public ToastViewModel ToastViewModel =>
        toastViewModel ??= App.Services!.GetRequiredService<ToastViewModel>();

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