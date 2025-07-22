using System;
using Microsoft.Extensions.DependencyInjection;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Device
{
    public partial class DevicesPageViewModel : ViewModelBase
    {
        private DevicesListViewModel? devicesList;
        private DeviceGroupsViewModel? deviceGroups;
        private readonly BE.DevicesModel devicesModel;

        public DevicesPageViewModel(userspace_backend.BackEnd backEnd)
        {
            devicesModel = backEnd?.Devices ?? throw new ArgumentNullException(nameof(backEnd));
        }

        public DevicesListViewModel DevicesList =>
            devicesList ??= new DevicesListViewModel(devicesModel);

        public DeviceGroupsViewModel DeviceGroups =>
            deviceGroups ??= new DeviceGroupsViewModel(devicesModel.DeviceGroups);

        protected BE.DevicesModel DevicesModel => devicesModel;
    }
}
