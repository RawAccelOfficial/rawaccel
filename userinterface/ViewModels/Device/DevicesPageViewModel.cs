using Microsoft.Extensions.DependencyInjection;
using System;
using userinterface.Services;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Device
{
    public partial class DevicesPageViewModel : ViewModelBase
    {
        private DevicesListViewModel? devicesList;
        private DeviceGroupsViewModel? deviceGroups;
        private readonly BE.DevicesModel devicesModel;
        private readonly IModalService modalService;
        private readonly LocalizationService localizationService;

        public DevicesPageViewModel(userspace_backend.BackEnd backEnd, IModalService modalService, LocalizationService localizationService)
        {
            devicesModel = backEnd?.Devices ?? throw new ArgumentNullException(nameof(backEnd));
            this.modalService = modalService;
            this.localizationService = localizationService;
        }

        public DevicesListViewModel DevicesList =>
            devicesList ??= new DevicesListViewModel(devicesModel, modalService, localizationService);

        public DeviceGroupsViewModel DeviceGroups =>
            deviceGroups ??= new DeviceGroupsViewModel(devicesModel.DeviceGroups);

        protected BE.DevicesModel DevicesModel => devicesModel;
    }
}