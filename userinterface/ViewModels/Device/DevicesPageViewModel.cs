using System;
using Microsoft.Extensions.DependencyInjection;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Device
{
    public partial class DevicesPageViewModel : ViewModelBase
    {
        private DevicesListViewModel? devicesList;
        private DeviceGroupsViewModel? deviceGroups;

        public DevicesPageViewModel()
        {
        }

        private BE.DevicesModel DevicesBE =>
            App.Services!.GetRequiredService<userspace_backend.BackEnd>().Devices;

        public DevicesListViewModel DevicesList =>
            devicesList ??= new DevicesListViewModel(DevicesBE);

        public DeviceGroupsViewModel DeviceGroups =>
            deviceGroups ??= new DeviceGroupsViewModel(DevicesBE.DeviceGroups);

        protected BE.DevicesModel DevicesModel => DevicesBE;
    }
}
