using userinterface.ViewModels.Device;
using userinterface.ViewModels.Mapping;
using userinterface.ViewModels.Profile;
using BE = userspace_backend;

namespace userinterface.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
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

    public void ApplyButtonClicked()
    {
        BackEnd.Apply();
    }
}
