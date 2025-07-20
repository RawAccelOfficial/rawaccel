using Avalonia;
using Avalonia.Styling;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using userinterface.Commands;
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
    private bool IsProfilesExpandedValue = false;

    // Lazy-loaded ViewModels
    private DevicesPageViewModel? devicesPage;
    private ProfilesPageViewModel? profilesPage;
    private MappingsPageViewModel? mappingsPage;
    private SettingsPageViewModel? settingsPage;
    private ProfileListViewModel? profileListView;
    private ToastViewModel? toastViewModel;

    private readonly BE.BackEnd backEnd;
    private readonly IThemeService themeService;

    public MainWindowViewModel(BE.BackEnd backEnd, IThemeService themeService)
    {
        this.backEnd = backEnd ?? throw new ArgumentNullException(nameof(backEnd));
        this.themeService = themeService ?? throw new ArgumentNullException(nameof(themeService));

        ApplyCommand = new RelayCommand(() => Apply());
        NavigateCommand = new RelayCommand<NavigationPage>(page => SelectPage(page));
        ToggleThemeCommand = new RelayCommand(() => ToggleTheme());

        SubscribeToProfileSelectionChanges();
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
        get => IsProfilesExpandedValue;
        set
        {
            if (IsProfilesExpandedValue != value)
            {
                IsProfilesExpandedValue = value;
                OnPropertyChanged();
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

    public void Apply()
    {
        BackEnd.Apply();
    }

    private void ToggleTheme()
    {
        var currentTheme = Application.Current?.ActualThemeVariant;
        var newTheme = currentTheme == ThemeVariant.Dark ? ThemeVariant.Light : ThemeVariant.Dark;
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = newTheme;
        }
        themeService.NotifyThemeChanged();
    }

    private void SubscribeToProfileSelectionChanges()
    {
        ProfileListView.ProfileItems.CollectionChanged += (sender, e) =>
        {
            if (e.NewItems != null)
            {
                foreach (ProfileListElementViewModel item in e.NewItems)
                {
                    item.SelectionChanged += OnProfileSelectionChanged;
                }
            }
            if (e.OldItems != null)
            {
                foreach (ProfileListElementViewModel item in e.OldItems)
                {
                    item.SelectionChanged -= OnProfileSelectionChanged;
                }
            }
        };

        foreach (var item in ProfileListView.ProfileItems)
        {
            item.SelectionChanged += OnProfileSelectionChanged;
        }
    }

    private void OnProfileSelectionChanged(ProfileListElementViewModel profileElement, bool isSelected)
    {
        if (isSelected)
        {
            ProfilesPage?.UpdateCurrentProfile();
        }
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual new void OnPropertyChanged([CallerMemberName] string? PropertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
    }

    public void Cleanup()
    {
        if (profileListView != null)
        {
            foreach (var item in profileListView.ProfileItems)
            {
                item.SelectionChanged -= OnProfileSelectionChanged;
            }
        }
    }
}