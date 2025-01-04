﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels
{
    public partial class DevicesPageViewModel : ViewModelBase
    {
        public DevicesPageViewModel(BE.DevicesModel devicesBE)
        {
            DevicesBE = devicesBE;
            DevicesList = new DevicesListViewModel(devicesBE);
            DeviceGroups = new DeviceGroupsViewModel(devicesBE.DeviceGroups);
        }

        public DevicesListViewModel DevicesList { get; }

        public DeviceGroupsViewModel DeviceGroups { get; }

        protected BE.DevicesModel DevicesBE { get; set; }
    }
}