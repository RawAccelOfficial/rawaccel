using System.Diagnostics;
using BE = userspace_backend.Model;

namespace userinterface.ViewModels.Device
{
    public partial class DeviceGroupViewModel : ViewModelBase
    {
        public DeviceGroupViewModel(BE.DeviceGroupModel deviceGroupBE, BE.DeviceGroups deviceGroupsBE)
        {
            DeviceGroupBE = deviceGroupBE;
            DeviceGroupsBE = deviceGroupsBE;
        }

        public BE.DeviceGroupModel DeviceGroupBE { get; }

        protected BE.DeviceGroups DeviceGroupsBE { get; }

        public void DeleteSelf()
        {
            bool success = DeviceGroupsBE.RemoveDeviceGroup(DeviceGroupBE);
            Debug.Assert(success);
        }
    }
}
