using System.ComponentModel;
using System.Runtime.CompilerServices;
using userinterface.Services;
using userinterface.ViewModels.Controls;
using userinterface.ViewModels.Device;
using userinterface.ViewModels.Mapping;
using userinterface.ViewModels.Profile;
using BE = userspace_backend;

namespace userinterface.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    private const string DefaultPage = "Devices";
    private const string DevicesPageName = "Devices";
    private const string MappingsPageName = "Mappings";
    private const string ProfilesPageName = "Profiles";

    private string SelectedPageValue = DefaultPage;
    private bool IsProfilesExpandedValue = false;

    public MainWindowViewModel(BE.BackEnd BackEnd, INotificationService notificationService, IModalService modalService)
    {
        this.BackEnd = BackEnd;
        DevicesPage = new DevicesPageViewModel(BackEnd.Devices);
        ToastViewModel = new ToastViewModel(notificationService);
        ProfileListView = new ProfileListViewModel(BackEnd.Profiles);
        ProfilesPage = new ProfilesPageViewModel(BackEnd.Profiles, ProfileListView, notificationService);
        MappingsPage = new MappingsPageViewModel(BackEnd.Mappings);

        ProfileListView.SelectionChangeAction = OnProfileSelectionChanged;
    }

    public DevicesPageViewModel DevicesPage { get; }

    public ProfilesPageViewModel ProfilesPage { get; }

    public MappingsPageViewModel MappingsPage { get; }

    public ProfileListViewModel ProfileListView { get; }

    public ToastViewModel ToastViewModel { get; }

    protected BE.BackEnd BackEnd { get; }

    public string SelectedPage
    {
        get => SelectedPageValue;
        set
        {
            if (SelectedPageValue != value)
            {
                SelectedPageValue = value;
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
            DevicesPageName => DevicesPage,
            MappingsPageName => MappingsPage,
            ProfilesPageName => ProfilesPage,
            _ => DevicesPage
        };

    public void SelectPage(string PageName)
    {
        SelectedPage = PageName;
        IsProfilesExpanded = PageName == ProfilesPageName;
    }

    public void Apply()
    {
        BackEnd.Apply();
    }

    private void OnProfileSelectionChanged()
    {
        ProfilesPage?.UpdateCurrentProfile();
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected virtual new void OnPropertyChanged([CallerMemberName] string? PropertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(PropertyName));
    }
}