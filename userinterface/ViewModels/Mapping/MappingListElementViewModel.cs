using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows.Input;
using userinterface.Commands;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Mapping;

public partial class MappingListElementViewModel : ViewModelBase
{
    private readonly BE.MappingGroup mappingGroup;
    private readonly BE.MappingModel parentMapping;

    public MappingListElementViewModel(BE.MappingGroup mappingGroup, BE.MappingModel parentMapping, bool isDefaultElement = false)
    {
        this.mappingGroup = mappingGroup;
        this.parentMapping = parentMapping;

        DeleteCommand = new RelayCommand(() => DeleteSelf());

        parentMapping.IndividualMappings.CollectionChanged += OnIndividualMappingsChanged;
    }

    public BE.MappingGroup MappingGroup => mappingGroup;

    public string DeviceGroupName => mappingGroup.DeviceGroup.CurrentValidatedValue;

    public ObservableCollection<BE.ProfileModel> AvailableProfiles => mappingGroup.Profiles.Profiles;

    public bool CanDelete => parentMapping.IndividualMappings.Count > 1;

    public BE.ProfileModel? SelectedProfile
    {
        get => mappingGroup.Profile;
        set
        {
            if (value != null && mappingGroup.Profile != value)
            {
                mappingGroup.Profile = value;
                OnPropertyChanged();
            }
        }
    }

    public bool ShowActionButtons => true;

    public ICommand DeleteCommand { get; }

    private void OnIndividualMappingsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(CanDelete));
    }

    public void DeleteSelf()
    {
        Cleanup();
        parentMapping.IndividualMappings.Remove(mappingGroup);
    }

    public void Cleanup()
    {
        parentMapping.IndividualMappings.CollectionChanged -= OnIndividualMappingsChanged;
    }
}