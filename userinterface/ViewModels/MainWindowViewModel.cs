using userinterface.ViewModels.Device;
using userinterface.ViewModels.Mapping;
using userinterface.ViewModels.Profile;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using BE = userspace_backend;

namespace userinterface.ViewModels;

public partial class MainWindowViewModel : ViewModelBase, INotifyPropertyChanged
{
    private string _selectedPage = "Devices";

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
    protected BE.BackEnd BackEnd { get; set; }

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

    public object? CurrentPageContent
    {
        get
        {
            return SelectedPage switch
            {
                "Devices" => DevicesPage,
                "Mappings" => MappingsPage,
                "Profiles" => ProfilesPage,
                _ => DevicesPage
            };
        }
    }

    public void SelectPage(string pageName)
    {
        SelectedPage = pageName;
    }

    public void ApplyButtonClicked()
    {
        BackEnd.Apply();
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
