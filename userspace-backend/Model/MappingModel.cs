using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DATA = userspace_backend.Data;
using userspace_backend.Model.AccelDefinitions;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Model.ProfileComponents;
using userspace_backend.Data;
using CommunityToolkit.Mvvm.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace userspace_backend.Model
{
    public class MappingModel : EditableSettingsCollection<DATA.Mapping>
    {
        public MappingModel(
            Mapping dataObject,
            IModelValueValidator<string> nameValidator,
            DeviceGroups deviceGroups,
            ProfilesModel profiles) : base(dataObject)
        {
            NameValidator = nameValidator;
            SetActive = false;
            DeviceGroups = deviceGroups;
            Profiles = profiles;
            InitIndividualMappings(dataObject);
            DeviceGroupsStillUnmapped = new ObservableCollection<DeviceGroupModel>();
            FindDeviceGroupsStillUnmapped();
            IndividualMappings.CollectionChanged += OnIndividualMappingsChanged;
            DeviceGroups.DeviceGroupModels.CollectionChanged += OnIndividualMappingsChanged;
        }

        private bool setActive;
        public bool SetActive 
        { 
            get => setActive; 
            internal set => SetProperty(ref setActive, value); 
        }

        public EditableSetting<string> Name { get; set; }

        public ObservableCollection<MappingGroup> IndividualMappings { get; protected set; } = null!;

        public ObservableCollection<DeviceGroupModel> DeviceGroupsStillUnmapped { get; protected set; } = null!;
        
        protected IModelValueValidator<string> NameValidator { get; }

        protected DeviceGroups DeviceGroups { get; }

        protected ProfilesModel Profiles { get; }

        public override Mapping MapToData()
        {
            Mapping mapping = new Mapping()
            {
                Name = Name.ModelValue,
                GroupsToProfiles = new Mapping.GroupsToProfilesMapping(),
            };

            foreach (var group in IndividualMappings)
            {
                mapping.GroupsToProfiles.Add(group.DeviceGroup.ModelValue, group.Profile.Name.ModelValue);
            }

            return mapping;
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return [];
        }

        protected override void InitEditableSettingsAndCollections(Mapping dataObject)
        {
            Name = new EditableSetting<string>(
                displayName: "Name",
                initialValue: dataObject.Name,
                parser: UserInputParsers.StringParser,
                validator: NameValidator);

            IndividualMappings = new ObservableCollection<MappingGroup>();
        }

        protected void InitIndividualMappings(Mapping dataObject)
        {
            foreach (var kvp in dataObject.GroupsToProfiles)
            {
                TryAddMapping(kvp.Key, kvp.Value);
            }
        }

        public bool TryAddMapping(string deviceGroupName, string profileName)
        {
            if (!DeviceGroups.TryGetDeviceGroup(deviceGroupName, out DeviceGroupModel? deviceGroup)
                || deviceGroup == null
                || IndividualMappings.Any(m => m.DeviceGroup.Equals(deviceGroup)))
            {
                return false;
            }

            if (!Profiles.TryGetProfile(profileName, out ProfileModel? profile)
                || profile == null)
            {
                return false;
            }

            MappingGroup group = new MappingGroup()
            {
                DeviceGroup = deviceGroup,
                Profile = profile,
                Profiles = Profiles,
            };

            IndividualMappings.Add(group);
            return true;
        }

        protected void FindDeviceGroupsStillUnmapped()
        {
            DeviceGroupsStillUnmapped.Clear();

            foreach (DeviceGroupModel group in DeviceGroups.DeviceGroupModels)
            {
                if (!IndividualMappings.Any(m => m.DeviceGroup.Equals(group)))
                {
                    DeviceGroupsStillUnmapped.Add(group);
                }
            }
        }

        protected void OnIndividualMappingsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            FindDeviceGroupsStillUnmapped();
        }
    }

    public class MappingGroup
    {
        public DeviceGroupModel DeviceGroup { get; set; }

        public ProfileModel Profile { get; set; }

        // This is here for easy binding
        public ProfilesModel Profiles { get; set; }
    }
}
