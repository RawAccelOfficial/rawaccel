using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Avalonia;
using Avalonia.Styling;
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

    public MainWindowViewModel(BE.BackEnd BackEnd, INotificationService notificationService, IModalService modalService, ISettingsService settingsService)
    {
        this.BackEnd = BackEnd;
        DevicesPage = new DevicesPageViewModel(BackEnd.Devices);
        ToastViewModel = new ToastViewModel(notificationService);
        ProfileListView = new ProfileListViewModel(BackEnd.Profiles);
        ProfilesPage = new ProfilesPageViewModel(BackEnd.Profiles, ProfileListView, notificationService);
        MappingsPage = new MappingsPageViewModel(BackEnd.Mappings);
        SettingsPage = new SettingsPageViewModel(settingsService);

        ApplyCommand = new RelayCommand(() => Apply());
        NavigateCommand = new RelayCommand<NavigationPage>(page => SelectPage(page));
        ToggleThemeCommand = new RelayCommand(() => ToggleTheme());

        SubscribeToProfileSelectionChanges();
    }

    public DevicesPageViewModel DevicesPage { get; }

    public ProfilesPageViewModel ProfilesPage { get; }

    public MappingsPageViewModel MappingsPage { get; }

    public SettingsPageViewModel SettingsPage { get; }

    public ProfileListViewModel ProfileListView { get; }

    public ToastViewModel ToastViewModel { get; }

    protected BE.BackEnd BackEnd { get; }

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
        foreach (var item in ProfileListView.ProfileItems)
        {
            item.SelectionChanged -= OnProfileSelectionChanged;
        }
    }
}