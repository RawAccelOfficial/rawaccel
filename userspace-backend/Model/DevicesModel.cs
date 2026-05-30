using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using userspace_backend.Data;
using userspace_backend.Model.EditableSettings;

namespace userspace_backend.Model
{
    public interface IDevicesModel : IEditableSettingsList<IDeviceModel, Device>
    {
    }

    public class DevicesModel : EditableSettingsList<IDeviceModel, Device>, IDevicesModel
    {
        public DevicesModel(
            IServiceProvider serviceProvider,
            ISystemDevicesProvider systemDevicesProvider,
            DeviceGroups deviceGroups)
            : base(serviceProvider, [], [])
        {
            SystemDevices = systemDevicesProvider;
            DeviceGroups = deviceGroups;
        }

        public DeviceGroups DeviceGroups { get; }

        public ISystemDevicesProvider SystemDevices { get; protected set; }

        protected override string DefaultNameTemplate => "Device";

        protected override string GetNameFromElement(IDeviceModel element)
        {
            return element.Name.ModelValue;
        }

        protected override void SetElementName(IDeviceModel element, string name)
        {
            element.Name.TryUpdateModelDirectly(name);
        }

        protected override string GetNameFromData(Device data)
        {
            return data.Name;
        }

        protected override bool TryMapEditableSettingsFromData(IEnumerable<Device> data)
        {
            return true;
        }
    }
}
