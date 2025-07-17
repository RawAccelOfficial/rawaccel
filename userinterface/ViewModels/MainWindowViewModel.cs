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
    private const NavigationPage DefaultPage = NavigationPage.Devices;
    private NavigationPage selectedPageValue = DefaultPage;
    private bool IsProfilesExpandedValue = false;

    // Lazy-loaded ViewModels
    private DevicesPageViewModel? devicesPage;
    private ProfilesPageViewModel? profilesPage;
    private MappingsPageViewModel? mappingsPage;
    private SettingsPageViewModel? settingsPage;
    private ProfileListViewModel? profileListView;
    private ToastViewModel? toastViewModel;

    private readonly BE.BackEnd backEnd;
    private readonly IServiceProvider serviceProvider;
    private readonly INotificationService notificationService;
    private readonly ModalService modalService;
    private readonly SettingsService settingsService;
    private readonly ILocalizationService localizationService;

    public MainWindowViewModel(
        BE.BackEnd backEnd,
        IServiceProvider serviceProvider,
        INotificationService notificationService,
        ModalService modalService,
        SettingsService settingsService,
        ILocalizationService localizationService)
    {
        this.backEnd = backEnd;
        this.serviceProvider = serviceProvider;
        this.notificationService = notificationService;
        this.modalService = modalService;
        this.settingsService = settingsService;
        this.localizationService = localizationService;

        ApplyCommand = new RelayCommand(() => Apply());
        NavigateCommand = new RelayCommand<NavigationPage>(page => SelectPage(page));
        ToggleThemeCommand = new RelayCommand(() => ToggleTheme());

        SubscribeToProfileSelectionChanges();
    }

    public DevicesPageViewModel DevicesPage =>
        devicesPage ??= new DevicesPageViewModel(backEnd.Devices);

    public ProfilesPageViewModel ProfilesPage =>
        profilesPage ??= new ProfilesPageViewModel(backEnd.Profiles, ProfileListView, notificationService);

    public MappingsPageViewModel MappingsPage =>
        mappingsPage ??= new MappingsPageViewModel(backEnd.Mappings);

    public SettingsPageViewModel SettingsPage =>
        settingsPage ??= new SettingsPageViewModel(settingsService);

    public ProfileListViewModel ProfileListView =>
        profileListView ??= new ProfileListViewModel(backEnd.Profiles);

    public ToastViewModel ToastViewModel =>
        toastViewModel ??= new ToastViewModel(notificationService);

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
        ThemeService.NotifyThemeChanged();
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