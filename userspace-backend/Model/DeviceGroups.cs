using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model
{
    public interface IDeviceGroups
    {
        bool TryGetDeviceGroup(string name, out string? deviceGroup);
    }

    public class DeviceGroups : EditableSettingsCollection<IEnumerable<string>>, IDeviceGroups
    {
        public const string DefaultDeviceGroup = "Default";

        public DeviceGroups(IEnumerable<string> devices)
            : base(devices)
        {
            GroupNameChangeValidator = new DeviceGroupValidator(this);
        }

        public ObservableCollection<string> DeviceGroupModels { get; set; }

        protected DeviceGroupValidator GroupNameChangeValidator { get; set; }

        public bool TryGetDeviceGroup(string name, out string? deviceGroup)
        {
            deviceGroup = DeviceGroupModels.FirstOrDefault(
                g => string.Equals(g, name, StringComparison.InvariantCultureIgnoreCase));

            return deviceGroup is not null;
        }

        public string AddOrGetDeviceGroup(string deviceGroupName)
        {
            if (!TryGetDeviceGroup(deviceGroupName, out string? deviceGroup))
            {
                deviceGroup = deviceGroupName;
                DeviceGroupModels.Add(deviceGroup);
            }

            return deviceGroup;
        }

        protected bool TryGetDefaultDeviceGroup([MaybeNullWhen(false)] out string defaultName)
        {
            for (int i = 0; i < 10; i++)
            {
                string nameToAdd = $"DeviceGroup{i}";
                if (TryGetDeviceGroup(nameToAdd, out _))
                {
                    continue;
                }

                defaultName = nameToAdd;
                return true;
            }

            defaultName = null;
            return false;
        }

        public bool TryAddDeviceGroup(string? deviceGroupName = null)
        {
            if (deviceGroupName is null)
            {
                if (!TryGetDefaultDeviceGroup(out var defaultName))
                {
                    return false;
                }

                deviceGroupName = defaultName;
            }
            else if (TryGetDeviceGroup(deviceGroupName, out _))
            {
                return false;
            }

            DeviceGroupModels.Add(deviceGroupName);
            return true;
        }

        public bool RemoveDeviceGroup(string deviceGroup)
        {
            return DeviceGroupModels.Remove(deviceGroup);
        }

        public override IEnumerable<string> MapToData()
        {
            return DeviceGroupModels;
        }

        protected override IEnumerable<IEditableSetting> EnumerateEditableSettings()
        {
            return [];
        }

        protected override IEnumerable<IEditableSettingsCollectionV2> EnumerateEditableSettingsCollections()
        {
            return [];
        }

        protected override void InitEditableSettingsAndCollections(IEnumerable<string> dataObject)
        {
            // Backend construction sets up the rest, pointing devices at their groups.
            DeviceGroupModels = new ObservableCollection<string>() { DefaultDeviceGroup };
        }
    }

    public class DeviceGroupValidator(DeviceGroups deviceGroups) : IModelValueValidator<string>
    {
        protected DeviceGroups DeviceGroups { get; } = deviceGroups;

        public bool Validate(string value)
        {
            return !DeviceGroups.TryGetDeviceGroup(value, out _);
        }
    }
}
