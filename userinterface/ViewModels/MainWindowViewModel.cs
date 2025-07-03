using System.ComponentModel;
using System.Runtime.CompilerServices;
using userinterface.Services;
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
    private string _selectedPage = DefaultPage;
    private bool _isProfilesExpanded = false;
    private readonly CurrentProfileService _currentProfileService;

    public MainWindowViewModel(BE.BackEnd backEnd, CurrentProfileService currentProfileService)
    {
        BackEnd = backEnd;
        _currentProfileService = currentProfileService;

        DevicesPage = new DevicesPageViewModel(backEnd.Devices);

        // Create ProfileListViewModel without callback since ProfilesPageViewModel will subscribe to service
        ProfileListView = new ProfileListViewModel(backEnd.Profiles, () => { }, _currentProfileService);
        ProfilesPage = new ProfilesPageViewModel(backEnd.Profiles, ProfileListView, _currentProfileService);

        MappingsPage = new MappingsPageViewModel(backEnd.Mappings);
    }

    public DevicesPageViewModel DevicesPage { get; }

    public ProfilesPageViewModel ProfilesPage { get; }

    public MappingsPageViewModel MappingsPage { get; }

    public ProfileListViewModel ProfileListView { get; }

    protected BE.BackEnd BackEnd { get; }

    public string SelectedPage
    {
        get => _selectedPage;
        set
        {
            if (_selectedPage != value)
            {
                _selectedPage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentPageContent));
            }
        }
    }

    public bool IsProfilesExpanded
    {
        get => _isProfilesExpanded;
        set
        {
            if (_isProfilesExpanded != value)
            {
                _isProfilesExpanded = value;
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

    public void SelectPage(string pageName)
    {
        SelectedPage = pageName;
        IsProfilesExpanded = pageName == ProfilesPageName;
    }

    public void ApplyButtonClicked()
    {
        var currentProfile = _currentProfileService.CurrentProfile;
        BackEnd.Apply(currentProfile);
    }

    public new event PropertyChangedEventHandler? PropertyChanged;
    protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
