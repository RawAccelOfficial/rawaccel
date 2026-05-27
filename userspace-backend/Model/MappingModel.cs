using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using userspace_backend.Model.EditableSettings;
using userspace_backend.Data;
using System.Collections.Specialized;

namespace userspace_backend.Model
{
    public interface IMappingModel : IEditableSettingsCollectionSpecific<Mapping>
    {
    }

    public class MappingModel: NamedEditableSettingsCollection<Mapping>, IMappingModel
    {
        public const string NameDIKey = $"{nameof(MappingModel)}.{nameof(Name)}";

        public MappingModel(
            IEditableSettingSpecific<string> name,
            IModelValueValidator<string> nameValidator,
            IDeviceGroups deviceGroups,
            IProfilesModel profiles,
            Mapping dataObject)
            : base(name, [], [])
        {
            NameValidator = nameValidator;
            SetActive = false;
            DeviceGroups = deviceGroups;
            Profiles = profiles;

            // Initialize collections
            IndividualMappings = new ObservableCollection<MappingGroup>();
            DeviceGroupsStillUnmapped = new ObservableCollection<string>();

            // Initialize mappings from data
            InitIndividualMappings(dataObject);
            FindDeviceGroupsStillUnmapped();

            // Wire up events
            IndividualMappings.CollectionChanged += OnIndividualMappingsChanged;
            if (DeviceGroups is DeviceGroups dg)
            {
                dg.DeviceGroupModels.CollectionChanged += OnIndividualMappingsChanged;
            }

            // When a referenced profile is deleted, reassign affected entries to the default profile.
            ((INotifyCollectionChanged)Profiles.Profiles).CollectionChanged += OnProfilesChanged;
        }

        private bool setActive;
        public bool SetActive 
        { 
            get => setActive; 
            internal set => SetProperty(ref setActive, value); 
        }

        public ObservableCollection<MappingGroup> IndividualMappings { get; protected set; }

        public ObservableCollection<string> DeviceGroupsStillUnmapped { get; protected set; }

        protected IModelValueValidator<string> NameValidator { get; }

        protected IDeviceGroups DeviceGroups { get; }

        protected IProfilesModel Profiles { get; }

        public override Mapping MapToData()
        {
            Mapping mapping = new Mapping()
            {
                Name = Name.ModelValue,
                GroupsToProfiles = new Mapping.GroupsToProfilesMapping(),
            };

            foreach (var group in IndividualMappings)
            {
                mapping.GroupsToProfiles.Add(group.DeviceGroup, group.Profile.Name.ModelValue);
            }

            return mapping;
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
            if (!DeviceGroups.TryGetDeviceGroup(deviceGroupName, out string? deviceGroup)
                || deviceGroup == null
                || IndividualMappings.Any(m => string.Equals(m.DeviceGroup, deviceGroup, StringComparison.InvariantCultureIgnoreCase)))
            {
                return false;
            }

            if (!Profiles.TryGetElement(profileName, out IProfileModel? profile)
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

            if (DeviceGroups is DeviceGroups dg)
            {
                foreach (string group in dg.DeviceGroupModels)
                {
                    if (!IndividualMappings.Any(m => string.Equals(m.DeviceGroup, group, StringComparison.InvariantCultureIgnoreCase)))
                    {
                        DeviceGroupsStillUnmapped.Add(group);
                    }
                }
            }
        }

        protected void OnIndividualMappingsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            FindDeviceGroupsStillUnmapped();
        }

        protected void OnProfilesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems == null)
            {
                return;
            }

            foreach (IProfileModel removed in e.OldItems.OfType<IProfileModel>())
            {
                ReassignGroupsFromRemovedProfile(removed);
            }
        }

        private void ReassignGroupsFromRemovedProfile(IProfileModel removedProfile)
        {
            IProfileModel? fallback = Profiles.DefaultProfile;

            // If the default profile itself was the one removed, fall back to any remaining profile.
            if (fallback == null || ReferenceEquals(fallback, removedProfile))
            {
                fallback = Profiles.Profiles.FirstOrDefault(p => !ReferenceEquals(p, removedProfile));
            }

            if (fallback == null)
            {
                return;
            }

            foreach (MappingGroup group in IndividualMappings)
            {
                if (ReferenceEquals(group.Profile, removedProfile))
                {
                    group.Profile = fallback;
                }
            }
        }

        protected override bool TryMapEditableSettingsFromData(Mapping data)
        {
            return Name.TryUpdateModelDirectly(data.Name);
        }

        protected override bool TryMapEditableSettingsCollectionsFromData(Mapping data)
        {
            return true;
        }
    }

    public class MappingGroup : ObservableObject
    {
        public string DeviceGroup { get; set; }

        private IProfileModel profile;

        public IProfileModel Profile
        {
            get => profile;
            set => SetProperty(ref profile, value);
        }

        // This is here for easy binding
        public IProfilesModel Profiles { get; set; }
    }
}
