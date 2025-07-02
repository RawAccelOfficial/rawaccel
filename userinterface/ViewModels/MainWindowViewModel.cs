using System.ComponentModel;
using System.Runtime.CompilerServices;
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

    public MainWindowViewModel(BE.BackEnd backEnd)
    {
        BackEnd = backEnd;
        DevicesPage = new DevicesPageViewModel(backEnd.Devices);
        ProfilesPage = new ProfilesPageViewModel(backEnd.Profiles);
        MappingsPage = new MappingsPageViewModel(backEnd.Mappings);
    }

    public DevicesPageViewModel DevicesPage { get; }

    public ProfilesPageViewModel ProfilesPage { get; }

    public MappingsPageViewModel MappingsPage { get; }

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

    public object? CurrentPageContent =>
        SelectedPage switch
        {
            DevicesPageName => DevicesPage,
            MappingsPageName => MappingsPage,
            ProfilesPageName => ProfilesPage,
            _ => DevicesPage
        };

    public void SelectPage(string pageName) => SelectedPage = pageName;

    public void ApplyButtonClicked() => BackEnd.Apply();

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected new virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
