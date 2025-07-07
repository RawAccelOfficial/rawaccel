using System.Collections.ObjectModel;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Mapping;

public partial class MappingListElementViewModel : ViewModelBase
{
    private readonly BE.MappingGroup mappingGroup;
    private readonly BE.MappingModel parentMapping;

    public MappingListElementViewModel(BE.MappingGroup mappingGroup, BE.MappingModel parentMapping)
    {
        this.mappingGroup = mappingGroup;
        this.parentMapping = parentMapping;
    }

    public BE.MappingGroup MappingGroup => mappingGroup;

    public string DeviceGroupName => mappingGroup.DeviceGroup.CurrentValidatedValue;

    public ObservableCollection<BE.ProfileModel> AvailableProfiles => mappingGroup.Profiles.Profiles;

    public BE.ProfileModel? SelectedProfile
    {
        get => mappingGroup.Profile;
        set
        {
            if (mappingGroup.Profile != value)
            {
                mappingGroup.Profile = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ShowActionButtons => true;

    public void DeleteSelf()
    {
        // Remove this mapping from the parent mapping
        parentMapping.IndividualMappings.Remove(mappingGroup);
    }
}