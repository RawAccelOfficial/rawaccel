using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using userspace_backend.Model.EditableSettings;
using DATA = userspace_backend.Data;

namespace userspace_backend.Model
{
    public class MappingsModel : EditableSettingsCollection<DATA.MappingSet>
    {
        private int activeMappingIndex;

        public MappingsModel(DATA.MappingSet dataObject, DeviceGroups deviceGroups, ProfilesModel profiles)
            : base(dataObject)
        {
            DeviceGroups = deviceGroups;
            Profiles = profiles;
            NameValidator = new MappingNameValidator(this);
            activeMappingIndex = -1;
            InitMappings(dataObject);
            LoadActiveMappingIndex(dataObject);
        }

        public ObservableCollection<MappingModel> Mappings { get; protected set; } = null!;

        public int ActiveMappingIndex 
        { 
            get => activeMappingIndex;
            private set
            {
                if (activeMappingIndex != value)
                {
                    UpdateActiveMappingStates(value);
                    activeMappingIndex = value;
                }
            }
        }

        protected DeviceGroups DeviceGroups { get; }

        protected ProfilesModel Profiles { get; }

        protected MappingNameValidator NameValidator { get; }

        public MappingModel GetMappingToSetActive()
        {
            return Mappings.FirstOrDefault(m => m.SetActive);
        }

        public MappingModel? GetActiveMapping()
        {
            if (ActiveMappingIndex >= 0 && ActiveMappingIndex < Mappings.Count)
            {
                return Mappings[ActiveMappingIndex];
            }
            return null;
        }

        public bool SetActiveMapping(MappingModel mapping)
        {
            int index = Mappings.IndexOf(mapping);
            if (index >= 0)
            {
                ActiveMappingIndex = index;
                return true;
            }
            return false;
        }

        public bool SetActiveMappingByIndex(int index)
        {
            if (index >= 0 && index < Mappings.Count)
            {
                ActiveMappingIndex = index;
                return true;
            }
            return false;
        }

        private void UpdateActiveMappingStates(int newActiveIndex)
        {
            for (int i = 0; i < Mappings.Count; i++)
            {
                Mappings[i].SetActive = (i == newActiveIndex);
            }
        }

        private void EnsureActiveMappingExists()
        {
            if (Mappings.Count > 0 && ActiveMappingIndex == -1)
            {
                ActiveMappingIndex = 0;
            }
        }

        private void LoadActiveMappingIndex(DATA.MappingSet dataObject)
        {
            if (dataObject != null && dataObject.ActiveMappingIndex >= 0 && dataObject.ActiveMappingIndex < Mappings.Count)
            {
                ActiveMappingIndex = dataObject.ActiveMappingIndex;
            }
            else
            {
                EnsureActiveMappingExists();
            }
        }

        public bool TryGetMapping(string name, out MappingModel? mapping)
        {
            mapping = Mappings.FirstOrDefault(
                m => string.Equals(m.Name.ModelValue, name, StringComparison.InvariantCultureIgnoreCase));

            return mapping is not null;
        }

        protected bool TryGetDefaultMapping([MaybeNullWhen(false)] out DATA.Mapping defaultMapping)
        {
            for (int i = 0; i < 10; i++)
            {
                string mappingNameToAdd = $"Mapping{i}";
                if (TryGetMapping(mappingNameToAdd, out _))
                {
                    continue;
                }

                defaultMapping = new()
                {
                    Name = mappingNameToAdd,
                    GroupsToProfiles = [],
                };

                return true;
            }

            defaultMapping = null;
            return false;
        }

        public bool TryAddMapping(DATA.Mapping? mappingToAdd = null)
        {
            if (mappingToAdd is null)
            {
                if (!TryGetDefaultMapping(out var defaultMapping))
                {
                    return false;
                }

                mappingToAdd = defaultMapping;
            }
            else if (TryGetMapping(mappingToAdd.Name, out _))
            {
                return false;
            }

            MappingModel mapping = new MappingModel(mappingToAdd, NameValidator, DeviceGroups, Profiles);
            Mappings.Add(mapping);
            EnsureActiveMappingExists();
            return true;
        }

        public bool RemoveMapping(MappingModel mapping)
        {
            int index = Mappings.IndexOf(mapping);
            if (index < 0) return false;
            
            bool removed = Mappings.Remove(mapping);
            if (removed)
            {
                if (index == ActiveMappingIndex)
                {
                    if (Mappings.Count > 0)
                    {
                        int newActiveIndex = Math.Min(index, Mappings.Count - 1);
                        ActiveMappingIndex = newActiveIndex;
                    }
                    else
                    {
                        activeMappingIndex = -1;
                    }
                }
                else if (index < ActiveMappingIndex)
                {
                    activeMappingIndex--;
                }
            }
            return removed;
        }

        public override DATA.MappingSet MapToData()
        {
            return new DATA.MappingSet()
            {
                Mappings = Mappings.Select(m => m.MapToData()).ToArray(),
                ActiveMappingIndex = ActiveMappingIndex
            };
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [];
        }

        protected override IEnumerable<IEditableSettingsCollection> EnumerateEditableSettingsCollections()
        {
            return Mappings;
        }

        protected override void InitEditableSettingsAndCollections(DATA.MappingSet dataObject)
        {
            Mappings = new ObservableCollection<MappingModel>();
        }

        protected void InitMappings(DATA.MappingSet dataObject)
        {
            foreach (DATA.Mapping mapping in dataObject?.Mappings ?? [])
            {
                TryAddMapping(mapping);
            }
        }
    }

    public class MappingNameValidator(MappingsModel mappings) : IModelValueValidator<string>
    {
        protected MappingsModel Mappings { get; } = mappings;
        public bool Validate(string value)
        {
            return !Mappings.TryGetMapping(value, out _);
        }
    }
}
